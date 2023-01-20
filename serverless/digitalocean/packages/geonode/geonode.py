from __future__ import annotations

import os
import requests

DO_KEYS = ['__ow_method', '__ow_headers', '__ow_path', 'http']
GEO_SERVER_URL = os.environ.get('GEO_SERVER_URL').rstrip('/')
GEO_SERVER_PORT = os.environ.get('GEO_SERVER_PORT')


def get_layer_params(headers: dict) -> list[str]:
    layer_params = {}
    for param in ['geo-layer', 'geo-token']:
        val = headers.get(param)
        if not val:
            return {
                'status': 400,
                'message': f'Bad request. Must contain `{param}` header',
            }
        layer_params[param] = val
    return list(layer_params.values())


def standardize_fields(fields: list[str]) -> list[str]:
    """
    Since GeoNode is unable to handle property names containing '/', we need
    to replace them with something, in this case '__'.
    """
    new_fields = {}
    for k, v in fields.items():
        if '/' in k:
            new_fields[k.replace('/', '__')] = v
        else:
            new_fields[k] = v

    return new_fields


def get_geo_xml(
    lat: float, lon: float, attrs: str, geo_layer: str, geo_server_url: str
) -> str:
    xml_template = '''<wfs:Transaction service="WFS" version="1.1.0" xmlns:wfs="http://www.opengis.net/wfs" xmlns:gml="http://www.opengis.net/gml" xmlns:ogc="http://www.opengis.net/ogc" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.opengis.net/wfs" xmlns:geonode="{geo_server_url}">
        <wfs:Insert>
            <geonode:{geo_layer}>
                {attrs}
                <geonode:the_geom>
                    <gml:Point srsDimension = "2" srsName = "EPSG:4326">
                        <gml:pos> {lon} {lat} </gml:pos>
                    </gml:Point>
                </geonode:the_geom>
            </geonode:{geo_layer}>
        </wfs:Insert>
    </wfs:Transaction>'''

    return xml_template.format(
        lat=lat,
        lon=lon,
        attrs=attrs,
        geo_layer=geo_layer,
        geo_server_url=geo_server_url,
    )


def main(args: dict):

    headers = args['http']['headers']
    geo_layer, geo_token = get_layer_params(headers)

    kobo_fields = {k: v for k, v in args.items() if k not in DO_KEYS}
    if not '_geolocation' in kobo_fields:
        return {
            'status': 400,
            'message': 'Bad request. Fields must contain `_geolocation`',
        }

    lat, lon = kobo_fields.pop('_geolocation')
    if not lat or not lon:
        return {
            'status': 400,
            'message': 'Bad request. Form must contain `geopoint` question type.',
        }

    fields = standardize_fields(kobo_fields)
    attrs = ''.join(
        f'<geonode:{k}>{v}</geonode:{k}>' for k, v in fields.items()
    )
    xml = get_geo_xml(
        lat=lat,
        lon=lon,
        attrs=attrs,
        geo_layer=geo_layer,
        geo_server_url=f'{GEO_SERVER_URL}/',
    )

    geo_url = (
        f'{GEO_SERVER_URL}:{GEO_SERVER_PORT}'
        if GEO_SERVER_PORT
        else GEO_SERVER_URL
    )
    url = f'{geo_url}/geoserver/ows?access_token={geo_token}'
    res = requests.post(
        url,
        data=xml,
        headers={'Content-Type': 'application/xml', 'encoding': 'UTF-8'},
    )

    return {'status': res.status_code}

import os
import requests
import xml.etree.ElementTree as ET

DO_KEYS = ['__ow_method', '__ow_headers', '__ow_path', 'http']
GEO_SERVER_URL = os.environ.get('GEO_SERVER_URL')
GEO_SERVER_PORT = os.environ.get('GEO_SERVER_PORT')
GEO_TOKEN = os.environ.get('GEO_TOKEN')


def main(args):

    headers = args['http']['headers']

    geo_layer = headers.get('geo-layer')
    if not geo_layer:
        return {
            'status': 400,
            'message': 'Bad request. Must contain `geo-layer` header',
        }

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

    # Since GeoNode is unable to handle property names containing '/', we need
    # to replace them with something, in this case '__'.
    clean_fields = {}
    for k, v in kobo_fields.items():
        if '/' in k:
            clean_fields[k.replace('/', '__')] = v
        else:
            clean_fields[k] = v

    attrs = ''.join(
        f'<geonode:{k}>{v}</geonode:{k}>' for k, v in clean_fields.items()
    )
    gsu = (
        GEO_SERVER_URL + '/'
        if not GEO_SERVER_URL.endswith('/')
        else GEO_SERVER_URL
    )

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

    xml = xml_template.format(
        lat=lat, lon=lon, attrs=attrs, geo_layer=geo_layer, geo_server_url=gsu
    )

    url = f'{GEO_SERVER_URL}:{GEO_SERVER_PORT}/geoserver/ows?access_token={GEO_TOKEN}'
    res = requests.post(
        url,
        data=xml,
        headers={'Content-Type': 'application/xml', 'encoding': 'UTF-8'},
    )

    return res

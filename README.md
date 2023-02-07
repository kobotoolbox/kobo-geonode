# Integrating KoboToolbox and GeoNode

This integration aims to connect your projects in KoboToolbox to the open source, web-based application [GeoNode](https://geonode.org/), a platform for developing geospatial information systems (GIS) and for deploying spatial data infrastructures (SDI). In order to connect the two platforms together, an external “integration layer” is necessary to pass information from a project in KoboToolbox, restructure it, and send it off to a GeoNode layer for plotting.

The connection relies on the [REST Service feature](https://support.kobotoolbox.org/rest_services.html) of KoboToolbox which allows for webhook connections with external applications via a URL. Once the REST Service has been configured in your project, each new submission will be pushed off to the external service, resulting in a one-way data connection. In this case, the external service, from your project’s perspective, is the integration layer which will ultimately complete the connection to your layer on the GeoNode server.

If your project is collecting geographic data using the [`geopoint` question type](https://xlsform.org/en/#gps) in your KoboToolbox form, and you are familiar with or have access to a GeoNode server, then this integration may aid your monitoring and analysis processes.

For advanced use-cases, forms can pull existing data from external CSV or XML files using the [`pulldata()`](https://xlsform.org/en/#how-to-pull-data-from-csv) method, and additionally use the KoboToolbox feature of [Dynamic Data Attachments](https://support.kobotoolbox.org/dynamic_data_attachment.html) for pre-populating fields based on previously collected data.

This tutorial will go through the following steps:
1. Creating a form in KoboToolbox
1. Creating a layer in GeoNode
1. Hosting the integration later
1. Configuring the project’s REST Service in KoboToolbox
1. Viewing submissions on GeoNode layer

Whether you start with creating your form in KoboToolbox or your layer in GeoNode is not important, however there are cases where you need to move between the different platforms as they depend upon each other.

_Note that this integration and tutorial has been developed specifically for DHI and their GeoNode server, using DigitalOcean as the hosting service. If changes are needed to enable your GeoNode server to use the integration or you would like to use a service other than DigitalOcean, please create a GitHub issue [here](https://github.com/kobotoolbox/kobo-geonode/issues/new) or a Pull Request with your changes._

## Creating a form in KoboToolbox

When creating a project in KoboToolbox and layer in GeoNode, it is essential that the question names in your form and layer attribute names match, otherwise the connection will not succeed  — _once the layer has been created, attributes cannot be added or modified_. For more details, refer to the GeoNode documentation [here](https://docs.geonode.org/en/3.2.x/usage/managing_layers/new_layer_from_scratch.html).

If you are just getting started with KoboToolbox, you can refer to the support article [here](https://support.kobotoolbox.org/overview_of_creating_a_project.html) on the basics of form creation in the Form Builder. In this example, we will use a simple form with the following questions (download the XLSForm [here](https://github.com/joshuaberetta/kobo-geonode/files/10674114/kobo-geonode.xlsx)):

| type        | name     | label                |
|-------------|----------|----------------------|
| geopoint    | location | Record your location |
| begin_group | person   | Person               |
| text        | name     | What is your name?   |
| integer     | age      | How old are you?     |
| end_group   |

The form will have the following appearance in the Enketo Web Form:
 
![image19](https://user-images.githubusercontent.com/50016008/217217423-7687218f-23d7-4935-99a0-ee896110cee1.png)

Once your form has been created, you can then click **DEPLOY** in the **FORM** tab of your project in KoboToolbox.

#### Handling grouped questions

If your form has grouped questions and you intend to have those fields included in the layer, it is important to note that the JSON representation of the question name includes the full path hierarchy, separated by forward slashes. For example, if we use the form shown above, and a response to the question of “What is your name” is “Bob”, and “How old are you?” is “42”, the JSON structure of the submission will be the following:

```json
{
  "person/name": "Bob",
  "person/age": 42
}
```

However, GeoNode attribute names cannot contain forward slashes, and therefore need to be accounted for in an alternative manner. When setting up the layer attributes, **group hierarchies must be separated with double underscores instead**. In the above example, we will set the attribute names to `person__name` and `person__age`.

_Note that repeat groups are not handled by this integration and cannot be included in the GeoNode layer_.

Attribute names can also only contain _lowercase_ letters. The integration layer converts all form question names to lowercase to account for this, while additionally replacing forward slashes with double underscores: a name of `Person/Name` will be sent to your GeoNode layer as `person__name`.

## Creating a layer in GeoNode

Log into your GeoNode profile. In the navigation menu at the top of the GeoNode interface, click on the _Data_ dropdown menu, and then “Create Layer.”

You will be directed to a page where layer details, attributes, permissions, etc. can be configured. Below is an example of a layer named “kobo_geonode” with “String” attributes of `person__name` and `person__age`, matching the form’s question names in KoboToolbox as shown in the previous section. _The double underscores account for the group hierarchy of the question._

_Note that only the “Points” Geometry type is currently supported in this integration._

![image8](https://user-images.githubusercontent.com/50016008/217217582-0969799c-0e3c-49a9-9078-3754088929b6.png)

After creating the layer, you can add it to any map or explore it as shown below: 

![image9](https://user-images.githubusercontent.com/50016008/217217693-a75c80b8-c7c0-4608-af83-6202d7f3885f.png)

### Obtain your GeoNode Access Token and Server URL

The GeoNode _Access Token_ is required for authenticating data coming from your KoboToolbox project to the GeoNode server, and the _Server URL_ is required for configuring the integration layer.

If you have an admin account on the GeoNode server, you can find your _Access Token_ by entering the Admin console:
1. Click on the dropdown menu in the top right corner of the interface where your name is displayed and then click on **Admin**.
1. Within the Admin console, navigate to the section titled “Django OAuth Toolkit” and then to “Access tokens” 
1. Within that page, you can find the token associated with your account, as well as all other user account tokens.

<img width="476" alt="image10" src="https://user-images.githubusercontent.com/50016008/217217764-24caf12c-ab63-4065-a064-52874bc81105.png">

If you have a non-admin user account, you can find your _Access Token_ through the following steps:
1. Click on the dropdown menu in the top right corner of the interface where your name is displayed and click on **Profile**. 
1. Click on the link titled “User layers WMS GetCapabilities document” and copy the `access_token` included in the URL of that page:

```
http://<geonode-url>/capabilities/user/<username>/?access_token=<your-access-token>
```

<img width="885" alt="image11" src="https://user-images.githubusercontent.com/50016008/217217864-5a95c949-ec24-4b62-8237-30c4546812a8.png">

Within the XML document at the above location, search for the term “geoserver/ows” which will be within an element named LegendURL. Copy the URL address:

```
http://<geo-server-url>:<port>/geoserver/ows
```

The values `<geo-server-url>` and `<port>` will be used to configure the integration’s environment variables.

## Hosting the integration layer

This section describes the necessary steps to publish the integration layer between KoboToolbox and a GeoNode server using the cloud service [DigitalOcean](https://www.digitalocean.com/). The process should be transferable to other cloud service providers, although may require minor code changes. The integration has been built to be deployed as either a [serverless function](https://www.digitalocean.com/blog/introducing-digitalocean-functions-serverless-computing) or a Dockerized application, each having their own advantages depending on the use-case.

### Option 1: Severless Function App

1. Fork the [kobotoolbox/kobo-geonode](https://github.com/kobotoolbox/kobo-geonode) repository to your own GitHub account.
2. After creating a project in DigitalOcean, click on **Create App**, or the **Create** dropdown menu and then **Apps**.

<img width="375" alt="image14" src="https://user-images.githubusercontent.com/50016008/217218253-17585586-49b0-4055-a678-c25b5d041d2d.png">

3. Select GitHub as the _Service Provider_, authenticate your account (if not already done) then choose `kobo-geonode` as the _Repository_, leaving the _Branch_ as `main`. Set the _Source Directory_ to `/serverless/digitalocean`. Once complete, click **Next**.

![image5](https://user-images.githubusercontent.com/50016008/217218060-ee7ebee1-bb4f-4244-bb31-d005a613cf48.png)

4. There is no need to configure resources in **Edit Plan**, however you can change the function’s name. In this example, we will use `kobo-geonode`. If you changed the name, click **Back** and then **Next**.

![image13](https://user-images.githubusercontent.com/50016008/217218375-a65280c8-61a8-4485-91aa-c190578540e2.png)

5. In the **Environment Variables** section, add the following variables in the `kobo-geonode` environment (encryption optional): `GEO_SERVER_URL`, `GEO_SERVER_PORT`. The values of these variables are found on the GeoNode server, as described in the section titled _**Obtain GeoNode Access Token and Server URL**_ above. Once complete, click **Save** and then **Next**.
	
![image12](https://user-images.githubusercontent.com/50016008/217218462-1e9b377b-a7c1-4765-bd32-c287d2e11d1b.png)

_Note that if you update any of the environment variables, you will need to deploy the app again by clicking on the **Actions** dropdown, and then **Deploy**._

6. Click **Next** in the **Info** section.
7. In the **Review** section, click **Create Resources** and wait for the application to build — this should take about a minute.
8. Once the application has finished building, there will be a URL in the _HTTP ROUTES_ section of the **Overview** tab, which will allow you to access the application from the internet. This URL is the endpoint for invoking the function and will be used directly in the REST Services configuration. For example: `https://goldfish-app-qwerty12345.ondigitalocean.app/geonode/geonode`

<img width="636" alt="image2" src="https://user-images.githubusercontent.com/50016008/217218582-ec0d04a7-f2e0-4e9e-878d-e5ca26fe5d9d.png">

### Option 2: Dockerized App

1. Fork the [kobotoolbox/kobo-geonode](https://github.com/kobotoolbox/kobo-geonode) repository to your own GitHub account.
2. After creating a project in DigitalOcean, click on **Create App**, or the **Create** dropdown menu and then **Apps**.

<img width="375" alt="image14" src="https://user-images.githubusercontent.com/50016008/217218766-a78c3ce6-f1ec-4f44-b2d6-e86a59399402.png">

3. Select GitHub as the _Service Provider_, authenticate your account (if not already done) then choose `kobo-geonode` as the _Repository_, leaving the _Branch_ as `main` and source directory as `/`. Once complete, click **Next**.

![image15](https://user-images.githubusercontent.com/50016008/217218825-e5058d18-f2b6-4f40-aa26-c4be7429fc4d.png)
	
4. This page allows you to edit the resources the application will use. For testing purposes or small loads, you can lower the monthly cost of the application by clicking **Edit Plan**, select _Basic_ as the plan type and then “_$5.00/mo - Basic_” as the resource size. Click **Back** and then **Next** to the following section.

![image6](https://user-images.githubusercontent.com/50016008/217218895-7db071e7-1062-4db7-a939-f6bd7a6d983a.png)

5. In the **Environment Variables** section, we will again set the following variables: `GEO_SERVER_URL`, `GEO_SERVER_PORT`. Click **Save** and then **Next**.
6. Click **Next** in the **Info** section.
7. In the **Review** section, click **Create Resources** and wait for the application to build, which may take a few minutes. You can watch the build logs in the **Activity** tab:

![image4](https://user-images.githubusercontent.com/50016008/217218983-c4cb8e3e-4d76-4932-a384-2a2c511b97e7.png)

8. Once the application has finished building, there will be a URL in the _HTTP ROUTES_ section of the **Overview** tab, which will allow you to access the application from the internet. We will use this URL when configuring the REST Service in KoboToolbox, pointing to the API endpoint of: `<your-app-url>/api/geonode`. For example, the full endpoint URL will be something like: `https://shark-app-qwerty12345.ondigitalocean.app/api/geonode`.

![image16](https://user-images.githubusercontent.com/50016008/217219033-982715b4-9505-4aac-a6ac-5cb68ab3d895.png)

## Configuring the project’s REST Service in KoboToolbox

Once the form has been created and deployed, the layer has been created in GeoNode and the integration later is live, we can finally configure the REST Service for the project in KoboToolbox.

1. Within your project in KoboToolbox, navigate to the **SETTINGS** tab and then to **REST Services**.

  ![image18](https://user-images.githubusercontent.com/50016008/217219173-1d42450c-7de6-486d-a99e-7a69e774665f.png)

2. Click on **REGISTER A NEW SERVICE** which will display a modal for configuring the service.

Within the modal, there are several fields which require values obtained in previous steps, such as the integration’s _endpoint URL_ from DigitalOcean (_Endpoint URL_ for the REST Service), and the _layer name_ and _Access Token_ from GeoNode which will be included in the _Custom HTTP Headers_ section as follows:

- geo-layer: `<layer name>`
- geo-token: `<Access Token>`

In the _Select fields subset_ section, it is important to **only include the fields that were created as attributes in the GeoNode layer**. This is with the exception of the field `_geolocation`, which **must be included** in the subset, and will be used to plot the point on the layer.

Based on the example layer, `kobo_geonode`, created earlier, the following fields must be included in the _Select fields subset_ section: 
- `_geolocation`
- `person/name`
- `person/age`

If we use the serverless function approach for the hosted integration layer on DigitalOcean, then here is an example of what the REST Service settings will be:

![image7](https://user-images.githubusercontent.com/50016008/217219291-fcf9751d-bfe6-425b-8012-598bb06297c5.png)

Once these fields have been configured, click **CREATE**.

## Viewing submissions on GeoNode layer

Within your project on KoboToolbox, navigate to the **FORM** tab, and then click **OPEN** within the _Collect data_ section. This will open the Enketo Web Form where you can create your first submission.

![image17](https://user-images.githubusercontent.com/50016008/217219374-b0deb999-a732-4ca7-a597-5d4162bbe61d.png)

Enter some values in the form and then click **Submit**.

![image3](https://user-images.githubusercontent.com/50016008/217219425-de3ae15b-4ae1-4407-8753-288c336aafc6.png)

Navigate back to your layer in GeoNode, refresh the page, and you should see a point displayed on the map. Click on the point, and our submission values will be displayed alongside:

![image1](https://user-images.githubusercontent.com/50016008/217219496-80ac78b5-5715-47b0-87e8-38fb798a6884.png)

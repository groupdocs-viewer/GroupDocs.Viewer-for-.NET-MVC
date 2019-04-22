![Alt text](https://raw.githubusercontent.com/groupdocs-viewer/groupdocs-viewer.github.io/master/resources/image/banner.png "GroupDocs.Viewer")
# GroupDocs.Viewer for .NET MVC Example
###### version 1.20.0

[![Build status](https://ci.appveyor.com/api/projects/status/6an4msspo1lh4two/branch/master?svg=true)](https://ci.appveyor.com/project/egorovpavel/groupdocs-viewer-for-net-mvc/branch/master)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/b9e1b19953654028b3afa801237fa66d)](https://www.codacy.com/app/GroupDocs/GroupDocs.Viewer-for-.NET-MVC?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=groupdocs-viewer/GroupDocs.Viewer-for-.NET-MVC&amp;utm_campaign=Badge_Grade)
[![GitHub license](https://img.shields.io/github/license/groupdocs-viewer/GroupDocs.Viewer-for-.NET-MVC.svg)](https://github.com/groupdocs-viewer/GroupDocs.Viewer-for-.NET-MVC/blob/master/LICENSE)

## System Requirements
- .NET Framework 4.5
- Visual Studio 2015


## Document viewer web application
This app is a **document viewer** that gives you the ability to view over 50 document formats including popular Office files (Word, Excel, PowerPoint) and many others directly in your browser. It is possible to **view documents as images or render them as HTML5**. Using [GroupDocs.Viewer for .NET](https://products.groupdocs.com/viewer/net) API  allows us to view any from 50 supported document formats without external dependencies and need to convert files to PDF first.

**Note** Without a license application will run in trial mode, purchase [GroupDocs.Viewer for .NET license](https://purchase.groupdocs.com/order-online-step-1-of-8.aspx) or request [GroupDocs.Viewer for .NET temporary license](https://purchase.groupdocs.com/temporary-license).


## Demo Video
[![Document viewer](https://raw.githubusercontent.com/groupdocs-viewer/groupdocs-viewer.github.io/master/resources/image/document-viewer-demo.gif)](https://www.youtube.com/watch?v=NnZaMNUC6o0)


## Features
- Clean, modern and intuitive design
- Easily switchable colour theme (create your own colour theme in 5 minutes)
- Responsive design
- Mobile support (open application on any mobile device)
- Support over 50 documents and image formats including **DOCX**, **PDF**, **PPT**, **XLS**
- HTML and image modes
- Fully customizable navigation panel
- Open password protected documents
- Text searching & highlighting
- Download documents
- Upload documents
- Print document
- Rotate pages
- Zoom in/out documents without quality loss in HTML mode
- Thumbnails
- Smooth page navigation
- Smooth document scrolling
- Preload pages for faster document rendering
- Multi-language support for displaying errors
- Display two or more pages side by side (when zooming out)
- Cross-browser support (Safari, Chrome, Opera, Firefox)
- Cross-platform support (Windows, Linux, MacOS)


## How to run

You can run this sample by one of following methods

#### Build from source

Download [source code](https://github.com/groupdocs-viewer/GroupDocs.Viewer-for-.NET-MVC/archive/master.zip) from github or clone this repository.

```bash
git clone https://github.com/groupdocs-viewer/GroupDocs.Viewer-for-.NET-MVC
```

Open solution in the VisualStudio.
Update common parameters in `web.config` and example related properties in the `configuration.yml` to meet your requirements.

Open http://localhost:8080/Viewer in your favorite browser

#### Docker image
Use [docker](https://www.docker.com/) image.

```bash
mkdir DocumentSamples
mkdir Licenses
docker run -p 8080:8080 --env application.hostAddress=localhost -v `pwd`/DocumentSamples:/home/groupdocs/app/DocumentSamples -v `pwd`/Licenses:/home/groupdocs/app/Licenses groupdocs/Viewer
## Open http://localhost:8080/Viewer in your favorite browser.
```

### Configuration
For all methods above you can adjust settings in `configuration.yml`. By default in this sample will lookup for license file in `./Licenses` folder, so you can simply put your license file in that folder or specify relative/absolute path by setting `licensePath` value in `configuration.yml`.

#### Viewer configuration options

| Option                 | Type    |   Default value   | Description                                                                                                                                  |
| ---------------------- | ------- |:-----------------:|:-------------------------------------------------------------------------------------------------------------------------------------------- |
| **`filesDirectory`**   | String  | `DocumentSamples` | Files directory path. Indicates where uploaded and predefined files are stored. It can be absolute or relative path                          |
| **`fontsDirectory`**   | String  |                   | Path to custom fonts directory.                                                                                                              |
| **`defaultDocument`**  | String  |                   | Absolute path to default document that will be loaded automaticaly.                                                                          |
| **`preloadPageCount`** | Integer |        `0`        | Indicate how many pages from a document should be loaded, remaining pages will be loaded on page scrolling.Set `0` to load all pages at once |
| **`htmlMode`**         | Boolean |      `true`       | HTML rendering mode. Set `false` to view documents in image mode. Set `true` to view documents in HTML mode                                  | 
| **`zoom`**             | Boolean |      `true`       | Enable or disable Document zoom                                                                                                              |
| **`search`**           | Boolean |      `true`       | Enable or disable document search                                                                                                            |
| **`thumbnails`**       | Boolean |      `true`       | Enable thumbnails preview                                                                                                                    |
| **`rotate`**           | Boolean |      `true`       | Enable individual page rotation functionality                                                                                                |
| **`cache`**            | Boolean |      `true`       | Set true to enable cache                                                                                                                     |
| **`saveRotateState`**  | Boolean |      `true`       | If enabled it will save chages made by rotating individual pages to same file.                                                               |
| **`watermarkText`**    | String  |                   | Text which will be used as a watermark                                                                                                       |


## License
The MIT License (MIT). Please have a look at the LICENSE.md for more details

## GroupDocs Document Viewer on other platforms

- JAVA DropWizard [Document Viewer](https://github.com/groupdocs-viewer/GroupDocs.Viewer-for-Java-Dropwizard) 
- JAVA Spring boot [Document viewer](https://github.com/groupdocs-viewer/GroupDocs.Viewer-for-Java-Spring)
- .NET WebForms [Document viewer](https://github.com/groupdocs-viewer/GroupDocs.Viewer-for-.NET-WebForms)

## Resources
- **Website:** [www.groupdocs.com](http://www.groupdocs.com)
- **Product Home:** [GroupDocs.Viewer for .NET](https://products.groupdocs.com/viewer/net)
- **Product API References:** [GroupDocs.Viewer for .NET API](https://apireference.groupdocs.com/net/viewer)
- **Download:** [Download GroupDocs.Viewer for .NET](http://downloads.groupdocs.com/viewer/net)
- **Documentation:** [GroupDocs.Viewer for .NET Documentation](https://docs.groupdocs.com/display/viewernet/Home)
- **Free Support Forum:** [GroupDocs.Viewer for .NET Free Support Forum](https://forum.groupdocs.com/c/viewer)
- **Paid Support Helpdesk:** [GroupDocs.Viewer for .NET Paid Support Helpdesk](https://helpdesk.groupdocs.com)
- **Blog:** [GroupDocs.Viewer for .NET Blog](https://blog.groupdocs.com/category/groupdocs-viewer-product-family/)

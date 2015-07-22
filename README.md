Visual Studio Extension for RobotDotNet
=======================================
<a href="https://www.myget.org/"><img src="https://www.myget.org/BuildSource/Badge/robotdotnet-build?identifier=a0a1e6b7-ad72-499d-87a4-2dbb5ab10784" alt="robotdotnet-build MyGet Build Status" /></a>

This is the source for a Visual Studio plugin for using RobotDotNet. Also includes preconfigured templates to help get started.

If you find any issues, please file them at https://github.com/robotdotnet/FRC-Extension/issues

Requirements
============

Visual Studio 2013 or Visual Studio 2015. Note that the express editions will not work. We recommend Visual Studio 2015 Community for the best experience.

Installation
============

In Visual Studio, go to Tools | Extensions and Updates. Then select online, and search for FRC Extension. Click on the extension, then click Download. It will download the extension, and then click Install to accept the eula and install the plugin.

Offline Installation
====================

The offline link to the extension can be found here.
https://visualstudiogallery.msdn.microsoft.com/7c7f4cd1-e4bc-43bb-a9f1-072c6f1197d9

To install offline, download this, and install the downloaded file.


Usage
=====

This plugin adds 3 things.

1. FRC Menu Item, which allows users to deploy robot code.

2. Project templates, which already include the WPILib references in them.

3. Item templates, of which there are 2 types. There are general item templates which are just general classes, which might include some special features. There are also command templates, which are used for creating your own commands.

Menu Extension
-------------


Project Templates
-----------------
When you create a new project, under the CSharp category, you will see an FRC menu. If you do not, make sure your .Net version is set to 4.5 or newer at the top of the new project window. Select one of these to create a new WPILib project.

Item Templates
--------------
When you are inside a project, if you right click on the project or a folder and select "Add New Item", in that menu there will be an FRC category where you can find the item templates.

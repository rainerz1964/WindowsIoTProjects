# WindowsIoTProjects
Using the Raspberry Pi 2 with WindowsIoT for Home Automation Stuff
# Project Details
Two solutions are within this project
## rfm69
### rfm69base
A library containing a driver for rfm69 and implmentations supporting intertechno and brennenstuhl plugs
The module is inspired by the Python based implementation
https://github.com/Phunkafizer/RaspyRFM
for further technical details please refer to
http://www.hoperf.com/upload/rf/RFM69CW-V1.1.pdf
### rfmWebAPI
A Web API for the rfm69 supporting switches using 433.92 MHz connected to a Raspberry pi running WindowsIoT based on Devkoes Restup Server
### rfm69
A Universal App similar to the one implemented in the BlinkyWebServer https://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm
## ExpressServerIoT
An Express Node.js based Web Servere using the rfmWebAPI to switch plugs defined by the rfmWebAPI
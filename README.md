PebbleSharp
===========

[![Build Status](https://travis-ci.org/brookpatten/PebbleSharp.Mono.BlueZ.svg?branch=master)](https://travis-ci.org/brookpatten/PebbleSharp.Mono.BlueZ)

This fork of PebbleSharp is intended specifically for use on mono with the bluez 5 bluetooth stack.
It is also meant for use on a raspberry pi (ARMHF), but that is *probably* not important, I believe it would work fine on any linux/bluez/mono installation.  (It definitely works on ubuntu)

If you are looking for wheezy or bluez 4 compatibility, you are looking for https://github.com/brookpatten/PebbleSharp/tree/e258667861a15d86ff1179c0f0dffe2acaa1b1f5

It also has additional functionality in pebblesharp.core for the following
* AppMessage Send/Receive
* Updated bundle format to support platform specific subdirectories
* BlobDB support for notifications
* BlobDB support for app installs
* Added various enums for things like hardware platform etc

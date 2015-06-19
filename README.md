PebbleSharp
===========

A C# library for interacting with the Pebble smart watch

This fork of PebbleSharp is intended specifically for use on mono with the bluez bluetooth stack.
It is also meant for use on a raspberry pi (ARMHF), but that is *probably* not important, I believe it would work fine on any linux/bluez/mono installation.

List of debian/rapsbian/ubuntu/whatever-ian packages required (work in progress... it's long)
* bluetooth
* blueman
* mono-complete

Build is straightfoward xbuild Pebble.sln on mono.

There are a couple tricks to getting it working.
* InTheHand.Net.Personal has rough support for linux/bluez, however it is not compiled in the release version of the code so thus unavailable in the nuget package.  You need to compile it yourself in debug mode for the bluez namespaces to be available.
* Mono sockets attempt to translate from the .net enum to the host OS enum for a given address family.  This causes a problem with opening a socket for bluetooth, this is detailed by the author of the inthehand/bluez implementation here: https://bugzilla.xamarin.com/show_bug.cgi?id=262
* To work around this issue you must pull the mono source, apply the afforementioned patch, and compile mono yourself. https://neildanson.wordpress.com/2013/12/10/building-mono-on-a-raspberry-pi-hard-float/

You must then pair the pebble to the pi using the blueman tool.  I have tried every way I know how to pair it from the shell but the blueman tool is the only thing that works, I do not know why this is.  You may need sudo the blueman tool (or x) for it to work.

Upon doing all this... it still won't work.... at least not fully.
Running pebblecmd will list the pebbles, (discovery works) but it is currently failing when it attempts to connect, that is the state of things right now.

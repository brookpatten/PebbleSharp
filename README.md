PebbleSharp
===========

A C# library for interacting with the Pebble smart watch, 100% stolen and hacked togethor from other open source projects.
If you're using this for anything real you're crazy, this is a total hack job just to get something working (and make it easy for me to transpot the code from my desktop=>pi and vice versa)

This fork of PebbleSharp is intended specifically for use on mono with the bluez bluetooth stack.
It is also meant for use on a raspberry pi (ARMHF), but that is *probably* not important, I believe it would work fine on any linux/bluez/mono installation.

There are a couple tricks to getting it working.
* InTheHand.Net.Personal has rough support for linux/bluez, however it is not compiled in the release version of the code so thus unavailable in the nuget package.  You need to compile it yourself in debug mode for the bluez namespaces to be available.  Additionally I found the struct offsets/packing were not lining up for whatever reason on raspbian or ubuntu.  I am unsure if this is due to how I compiled mono or just how bluez etc was compiled.
* Mono sockets attempt to translate from the .net enum to the host OS enum for a given address family.  This causes a problem with opening a socket for bluetooth, this is detailed by the author of the inthehand/bluez implementation here: https://bugzilla.xamarin.com/show_bug.cgi?id=262 .  To work around this issue you must pull the mono source, apply the afforementioned patch, and compile mono yourself. https://neildanson.wordpress.com/2013/12/10/building-mono-on-a-raspberry-pi-hard-float/

===========
Here is a step by step breakdown of how to get raspbian wheezy setup such that this will run.

//add the mono package repos (we don't use the ones from debian because they are super old)
* sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
* echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
* echo "deb-src http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
* echo "deb http://download.mono-project.com/repo/debian wheezy-libjpeg62-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
* echo "deb-src http://download.mono-project.com/repo/debian wheezy-libjpeg62-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list

* sudo apt-get install mono-complete monodevelop

//pebblesharp
* cd /home/pi
* mkdir dev
* cd dev
* git clone https://github.com/brookpatten/PebbleSharp

//patch mono
* sudo apt-get install git autoconf libtool automake build-essential gettext
* sudo apt-get source mono
* cp PebbleSharp/InTheHand.Net.Personal/MonoPatch/socket_BTH_2.patch ./<mono>
* cd <mono>
* git apply socket_BTH_2.patch
* ./autogen.sh
* make
* sudo make install

//fix the gac for monodevelop
* cd /usr/lib/mono/gac/
* sudo gacutil -i glib-sharp/2.12.0.0__35e10195dab3c99f/glib-sharp.dll &&
* sudo gacutil -i atk-sharp/2.12.0.0__35e10195dab3c99f/atk-sharp.dll &&
* sudo gacutil -i gdk-sharp/2.12.0.0__35e10195dab3c99f/gdk-sharp.dll &&
* sudo gacutil -i gtk-sharp/2.12.0.0__35e10195dab3c99f/gtk-sharp.dll &&
* sudo gacutil -i glade-sharp/2.12.0.0__35e10195dab3c99f/glade-sharp.dll &&
* sudo gacutil -i pango-sharp/2.12.0.0__35e10195dab3c99f/pango-sharp.dll &&
* sudo gacutil -i gnome-sharp/2.24.0.0__35e10195dab3c99f/gnome-sharp.dll && 
* sudo gacutil -i gconf-sharp/2.24.0.0__35e10195dab3c99f/gconf-sharp.dll &&
* sudo gacutil -i gnome-vfs-sharp/2.24.0.0__35e10195dab3c99f/gnome-vfs-sharp.dll

//bluetooth setup (some of these might not actually be needed)
* sudo apt-get install libdbus-1-dev libdbus-glib-1-dev libglib2.0-dev libical-dev libreadline-dev libudev-dev libusb-dev bluetooth blueman python-dev libopenobex1-dev python-tk python-bluez libbluetooth-dev bluez-compat

* at this point start x windows (startx), open a terminal, and run sudo blueman-manager
* search for devices
* on the pebble go to bluetooth menu to make it discoverable
* select the device in blueman-manager and pair, then trust

//setup rfcomm
* sudo nano /etc/bluetooth/rfcomm.conf
* uncomment the sample device and add the mac for your pebble(s)
* sudo rfcomm release 0
* sudo rfcomm bind 0

//fix bug in monodevelop so it doesn't hog up file handles
* edit monodevelop launch script
* export MONO_MANAGED_WATCHER=disabled

//pebblesharp
* cd /home/pi/dev/PebbleSharp
* xbuild /p:Configuration=Debug PebbleSharp.sln
* sudo mono /home/pi/dev/PebbleSharp/PebbleCmd/bin/Debug/PebbleCmd.exe

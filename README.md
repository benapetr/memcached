memcached
=========

Memcached server written in c# - with simpler configuration and setup and with many more options than the original version has, including plain text authentication and separate cache per user

Build
=====

Download the source code, enter the directory and type:

 xbuild

Please note that you need to have a mono compiler and runtime available on your system

Installation
============

Once you build the binary, it is recommended to create shell script wrapper like

    #!/bin/sh
    mono memcached.exe $*

save it as memcached in the build folder and then you can type

./memcached -h # to display help
./memcached # to start memcached server with default options
./memcached --print-conf # to print a configuration file
./memcached --config-file # to load a config file

Managing users
==============

Users are stored in a user db file, which is a plain text file, this file contains user names and passwords so make sure to not make it readable by other users than system user which memcache run as

example:

    user:12
    bob:hi

creates 2 users, the file has syntax in format user:password you can change this file while daemon is running

The file is stored by default as users in working directory, but it can be changed by configuration option userdb

Features
========

This is a memcache server which support many additional features, primarily it supports authentication and separate caches per user, that means you can create one instance of memcache server and offer it to multiple users who can't access the data of other users

Biggest advantage of this version is that is support different memory hashtables (separate memory stores) and each user has own dedicated hashtable. There is also a shared memory for everyone.

The memory limit and usage can be displayed per user

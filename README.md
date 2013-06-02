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

Authentication
==============

The memcached has authentication enabled by default, that means, every user has to login before they can write or read the memory, even the shared one. In order to do that you need to provide the authenticate command, for example

    authenticate bob:password\r\n

the response will be either SUCCESS when you log in successfuly or ERROR (ERROR01)

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

Descriptive errors
==================
You can enable this in configuration in order to get more descriptive errors which provide you the explanation of what is wrong, instead of ERROR you will receive ERRORNN with number of error. This is disabled by default.

Table
    ERROR00 Internal error (this means some kind of exception happened inside of server)
    ERROR01 Authentication failed (wrong username or password)
    ERROR02 Authentication required (you provided a correct command but you don't have permissions to read or write)
    ERROR03 Uknown request - the command is not understood by server
    ERROR04 Out of memory - there isn't enough operating memory to store the data, or you exceeded the limit
    ERROR05 Invalid values one of the values you provided has incorrect format
    ERROR06 Missing values - you need to provide all parameters
    ERROR07 Value is too big

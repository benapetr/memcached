memcached
=========

Memcached server written in c# - with simpler configuration and setup and with many more options than the original version has, including plain text authentication and separate cache per user

Build
=====

Download the source code, enter the directory and type:

 xbuild

Please note that you need to have a mono compiler and runtime available on your system

Features
========

This is a memcache server which support many additional features, primarily it supports authentication and separate caches per user, that means you can create one instance of memcache server and offer it to multiple users who can't access the data of other users

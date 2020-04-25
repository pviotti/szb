# Sizy

[![.NET Core CI](https://github.com/pviotti/sizy/workflows/.NET%20Core%20CI/badge.svg)](https://github.com/pviotti/sizy/actions?query=workflow%3A%22.NET+Core+CI%22+branch%3Amaster)

Sizy is a small cross-platform application that helps you get a quick
estimation of the disk space usage.

:warning: **WARNING** :warning: : this is an alpha-grade work-in-progress project.
I plan to add a graphical user interface (similar to Gnome Disk Usage Analyzer)
or a ncurses-like command line menu to browse folders (similar to Ranger file
manager). Feel free to reach out and open issues or prepare PR if you want to
contribute. :nerd_face:

## GUI Usage

 - `Return` or `right cursor`: to browse into a directory
 - `b` or `left cursor`: to browse back into the parent directory
 - `d`: delete file or directory (requires confirmation)
 - `q`: exit
 - `?`: show command help
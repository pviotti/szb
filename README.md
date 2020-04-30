# Sizy

[![.NET Core CI](https://github.com/pviotti/sizy/workflows/.NET%20Core%20CI/badge.svg)](https://github.com/pviotti/sizy/actions?query=workflow%3A%22.NET+Core+CI%22+branch%3Amaster)

Sizy helps answering this question quickly: *what files or folders are eating up my disk space?*

Through a text-based user interface, it lets you browse the file system tree
and delete the useless folders and files that take up more space.

:warning: **WARNING** :warning: : this is an alpha-grade work-in-progress project.
Feel free to reach out and open issues or prepare PR if you want to
contribute. :nerd_face:

## Usage

![screenshot](https://user-images.githubusercontent.com/1350095/80761044-bd12b680-8b31-11ea-991d-fd54a8ed77fe.png)

To only print the result for the input folder, you can use the `-p` flag.  
These are the command line options currently available:

```
USAGE: sizy [--help] [--version] [--print-only] [<path>]

INPUT:

    <path>                the folder you want to analyse (default: current folder).

OPTIONS:

    --version             print sizy version.
    --print-only, -p      output the results to screen and exit.
    --help                display this list of options.
```

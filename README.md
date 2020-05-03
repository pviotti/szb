# Sizy

[![.NET Core CI](https://github.com/pviotti/sizy/workflows/.NET%20Core%20CI/badge.svg)](https://github.com/pviotti/sizy/actions?query=workflow%3A%22.NET+Core+CI%22+branch%3Amaster)

Sizy helps answering this question quickly: *what files or folders are eating up my disk space?*

Through a text-based user interface similar to [mc] or [ranger], it lets you browse the file system tree
and delete the useless folders and files that take up space.<sup>[1](#myfootnote1)</sup>

:warning: **WARNING** :warning: : this is an alpha-grade work-in-progress project.
Feel free to reach out and open issues or prepare PR if you want to
contribute. :nerd_face:

## Usage

![screenshot](https://user-images.githubusercontent.com/1350095/80761044-bd12b680-8b31-11ea-991d-fd54a8ed77fe.png)

To only print the result for the input folder, you can use the `-p` flag.

```
USAGE: sizy [--help] [--version] [--print-only] [<path>]

INPUT:

    <path>                the folder you want to analyse (default: current folder).

OPTIONS:

    --version             print sizy version.
    --print-only, -p      output the results to screen and exit.
    --help                display this list of options.
```

----
<small><a name="myfootnote1">1</a>: Notice that, as suggested [here][ranger-issue], 
with ranger you can achieve more or less the same result, but just for a single
level in the file system hierarchy. Besides ranger doesn't run natively on Windows.</small>


 [mc]: https://en.wikipedia.org/wiki/Midnight_Commander
 [ranger]: https://ranger.github.io/
 [ranger-issue]: https://github.com/ranger/ranger/issues/719

# Sizable

[![.NET CI](https://github.com/pviotti/szb/workflows/.NET%20CI/badge.svg)](https://github.com/pviotti/szb/actions?query=workflow%3A%22.NET+CI%22+branch%3Amaster)

Sizable (*szb*) helps answering this question quickly: *what files or folders are eating up my disk space?*

Through a text-based user interface similar to [mc] or [ranger], it lets you browse the file system tree
and delete the useless files and directories that take up space.<sup>[1](#myfootnote1)</sup>

:warning: **WARNING** :warning: : this is a work-in-progress project.
Feel free to reach out and open issues or prepare PR if you want to
contribute. :nerd_face:

:newspaper: **UPDATE** :newspaper: : I found [ncdu], which does much of what I need,
so I don't think I'll actively work on this anymore.

## Usage

![screenshot](https://user-images.githubusercontent.com/1350095/100484430-499b4c80-30f4-11eb-8504-0129cc6d9ebb.png)

To only print the result for the input folder, you can use the `-p` flag.

```
USAGE: szb [--help] [--version] [--print-only] [<path>]

INPUT:

    <path>                the directory to analyse (default: current directory)

OPTIONS:

    --version             print szb version
    --print-only, -p      output the results to screen and exit
    --help                display this list of options.
```

----
<sub><a name="myfootnote1">1</a>: Notice that, as suggested [here][ranger-issue],
with ranger you can achieve more or less the same result, but just for a single
level in the file system hierarchy. Besides ranger doesn't run natively on Windows.</sub>


 [mc]: https://en.wikipedia.org/wiki/Midnight_Commander
 [ranger]: https://ranger.github.io/
 [ranger-issue]: https://github.com/ranger/ranger/issues/719
 [ncdu]: https://code.blicky.net/yorhel/ncdu

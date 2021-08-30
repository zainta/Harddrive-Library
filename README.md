# Harddrive-Library

## Introduction
The Hard Drive Library (HDL) is a utility that allows easy searching for any item within the observed storage media.  Currently, the HDL is being implemented solely as a commandline utility, however, in the future, a WPF GUI application will be written.  Both will provide similar functionality.

The system's main benefit is the implementation of a query language that allows filtering of files based on various properties (by size, last date access, etc).  This language will be further enhanced throughout development.

## Usage
The command utility supports the following parameters:
* hdsl -db: `<database file name>`
  * Directs the utility to use the database file (or create one) at the given location with the given name.
  * Defaults to creating `files database.db` in the utility's containing folder.
* hdsl -scan: `<path>[, <path>, <path>]`
  * Performs a location scan to populate the database on the given location.  Any number of paths can be provided.
* hdsl -run: `<HDSL script string>`
  * Executes the given HDSL code against the current databasee and outputs the results to the command prompt.
* hdsl -exec: `<HDSL script file path>`
  * Executes the script in the indicated file and outputs the results to the command prompt.
* hdsl -columns: `a column format string, see below`
  * Supported columns (all dates are in local time):
    * l => location
    * p => full path
    * n => directory or path name
    * e => extension
    * i => is file (y/n)
    * s => size
    * w => last write date
    * a => last access date
    * c => creation date
  * Column Format Strings (use either, but not both)
    * -columns: `<any combination of column letters, e.g "psc" (quotes optional) for the full path, followed by size and finally creation date>`
    * -columns: `<a comma seperated series of letter keys followed by a colon and then a number, where that number is the width of the column.  e.g "p:100,s:40,c:70" (no quotes)>`

Parameters are always handled in the following order:
* -db
* -scan
* -run
* -exec

This allows a scan to be performed immediately followed by a script execution.

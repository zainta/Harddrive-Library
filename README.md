# Harddrive-Library

## Introduction
The Hard Drive Library (HDL) is a utility that allows easy searching for any item within the observed storage media.  Currently, the HDL is being implemented solely as a commandline utility, however, in the future, a WPF GUI application will be written.  Both will provide similar functionality.

The system's main benefit is the implementation of a query language that allows filtering of files based on various properties (by size, last date access, etc).  This language will be further enhanced throughout development.

## Usage

### Options
The command utility supports the following parameters:
* hdsl -db: `<database full path>`
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
* hdsl -paging: `<a paging string in the form [n]:[n]>`, where n is a non-negative integer.
  * The first value is the page index.  If omitted, all pages are displayed.
  * The second value is the number of rows per page.  If omitted, defaults to 32.
* hdsl -dm: p/s/t/q
  * Sets the display mode for any scans performed.  SUpports the following parameters:
    * p - displays a progress bar representing scan progress.
    * s - displays a spinner to assure the user that the application is running.
    * t - outputs running textual log of activities.
    * q - executes without producing any output.

Parameters are always handled in the following order:
* -db
* -scan
* -run
* -exec

This allows a scan to be performed immediately followed by a script execution.

### Flags 
HDSL supports flags to assist with its output and behavior.  Note that all flags are toggles; if they default to true, setting them will set them to false.  Unless otherwise noted, all flags are nestable, i.e -hdsl, where h, d, s, and l are each seperate flags.
* e - Embellish - defaults to on
  * When on, query results display column names for all represented columns immediately and at the top of each page.
  * When off, no headers are shown, and columns are seperated by tabs instead of pipes.
* c - Count Results - defaults to on
  * When on, the number of records returned by a query will be output after the queries results.
* s - Update ini file - defaults to off
  * When on, rewrites the ini file with the value provided for the database path.  Either the ini file value if the -db option is omitted, or the new value provided through -db.

## The Hard Drive Search Language
HDSL is a simple query language designed for the retrieval of files and directories based on their locations and characteristics.  The system currently implements the following statements:
 * `find [file search pattern - defaults to *.*] [in [path[, path, path]] - defaults to current] [where clause];`
   * Retrieves the items that match the query and displays them.
 * `purge [where clause];`
   * Removes matching entries from the current database.
 * `[Bookmark] = '<absolute directory path string>';`
   * Creates a bookmark reference.
   * Bookmarks can be substituted for scan and find location.
 * `scan [spinner|progress|text|quiet - defaults to text] [path[, path, path]];`
   * Performs a disk item scan with the requested display mode on the provided paths  

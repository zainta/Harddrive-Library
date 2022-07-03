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
* hdsl -dm: p/s/t/q
  * Sets the display mode for any scans performed.  Supports the following parameters:
    * p - displays a progress bar representing scan progress.
    * s - displays a spinner to assure the user that the application is running.
    * t - outputs running textual log of activities.
    * q - executes without producing any output.
* hdsl -help: o/l/f/s/h (or any combination)
  * Displays help for specific topics.
    * o - Commandline option documentation.
    * s - Commandline option shortcut documentation.
    * l - HDSL statement documentation.
    * f - Commandline flag documentation.
    * h - Help command documentation.

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
* r - Remote service usage - defaults to on
  * When on, the console utility will attempt to query the configured service endpoints for results rather than running them itself.

### Shortcuts
HDSL offers a shorthand parameter alternative format that, while less legible, allows another method of providing the same parameters.
* hdsl ex'`<hdsl file path>`'
  * A shorthand invocation of the -exec parameter.

## The Hard Drive Search Language
HDSL is a simple query language designed for the retrieval of files and directories based on their locations and characteristics.  The system currently implements the following statements:
 * `find [filesystem - default] [columns columnref[, columnref]] [in/within/under [path[, path, path]]] [where clause] [group clause] [order clause] [paging clause];` or
   `find [wards | watches | hashlogs] [columns columnref[, columnref]] [path[, path, path]] [where clause] [group clause] [order clause] [paging clause];`
   * Retrieves the items that match the query and displays them.
   * The following statements all do the same thing -- they search for .dll files in C:\Windows\System32 that are under 1mb in size.
     * e.g `find in 'C:\Windows\System32' where Size < 1024000 and Extension = '.dll';`
     * e.g `find under 'C:\Windows\System32' where Size < 1024000 and Path ~ '.*\.dll$';`
 * `purge [bookmarks | exclusions | watches | wards | hashlogs] | [path[, path, path] [where clause] [group clause] [order clause]];`
   * Removes matching entries from the current database.
   * e.g `purge;` deletes all file tracking records from the database
   * e.g `purge exclusions;` deletes all exclusions, etc.
 * `[Bookmark] = '<absolute directory path string>';`
   * Creates a bookmark reference.
   * Bookmarks can be substituted for scan, exclusion definition, purge, and find paths.
   * e.g `[winSys] = 'C:\Windows\System';` creates a bookmark to the C:\Windows\System directory.
 * `scan [spinner|progress|text|quiet - defaults to text] [path[, path, path]];`
   * Performs a disk item scan with the requested display mode on the provided paths.  
   * e.g `scan text 'C:\';` will scan the entire C: drive and output progress to the console as text.
 * `check [spinner|progress|text|quiet - defaults to text] [columns columnref[, columnref]] [in/within/under] [path[, path, path] - defaults to current] [where clause] [group clause] [order clause];`
   * Performs a `find` query and then executes an integrity scan on the results of the query.
   * Compares existing hashes to the database to newly generated ones.  If no previous integrity check has been performed, simply stores the values in the database.
   * e.g `scan text 'C:\';` will perform an integrity check on the entire C: drive and output progress to the console as text.
 * `exclude [dynamic] path[, path, path];` 
   * Adds an exclusion for the given items.  Accepts file(s) and/or path(s).
   * The `dynamic` keyword causes any bookmarks in an exclusion to be evaluated when the exclusion is enforced, as opposed to when it is created.
   * e.g `exclude [Win];` will be stored as an exclusion of the specified value of the `[Win]` bookmark, where as `exclude dynamic [win];` will store the bookmark text.  This means that any changes made to [win] will automatically be picked up.
 * `include path[, path, path];`
   * Deletes exclusions for the given paths
   * e.g `include [win];` will remove the previous example.  Note that exclusions are not cascaded.
 * `--` creates a line comment.
 * `/* some text */` creates a multi-line comment.
 * `watch [passive] [refresh time] [path[, path, path] - defaults to current];`
   * Creates a watch for each of the given paths.  
   * A watch performs an initial scan and then passively monitors location for activity, updating the database when any is detected.
   * If a refresh time is provided, the initial scan is repeated every day at the given time.
     * Note that refresh times use 24 hour notation.  e.g `0:0:0` is midnight, and `12:0:0` is noon.
   * The passive keyword causes the watch to start in passive mode and skip the initial scan.
   * e.g `watch 'C:\';` will watch the entire C: drive, automatically updating when changes occur after the initial scan.  No repeat scans are performed.
   * e.g `watch 0:0:0 'C:\';` will watch the entire C: drive, automatically updating when changes occur after the initial scan and performing a fresh scan at midnight, every night.
 * `ward (time interval) [in/within/under [path[, path, path]] - defaults to current] [where clause] [group clause] [order clause];`
   * Performs an immediate integrity check and then successive ones whenever the interval expires.
   * e.g `ward 5::: under 'C:\Windows\System32' where extension = '.dll' and +system;` will create a ward to perform an integrity check on .dll system files every 5 days.
   * Time Intervals use the following syntax `d:h:m:s` where each number is optional, but the colons are required for meaning determination.
     * e.g `5:::` means 5 days.
     * e.g `10::` means 10 hours.
     * e.g `1::2:30` means 1 day, 2 minutes, and 30 seconds.
 * The `set` statement
   * `set out | standard | error path;`
     * Redirects the console's output to the provided path.
     * Using `standard` changes the standard output, `error` the error, and `out` will change both.
     * e.g `set standard @'C:\HDSL\activity.log';` will reroute standard output to the C:\HDSL\Activity.log file.
   * `set alias [filesystem | wards | watches | hashlogs] columnref, span (width in characters);`
     * Changes the display width for the given column.
     * e.g `set filesystem Size, span 10;` will set the display width of the Size column to 10 characters.
   * `set alias [filesystem | wards | watches | hashlogs] columnref, 'new alias string';`
     * Changes the alias for the given column to the new one provided.
     * e.g `set filesystem Size, 'Size';` will change the Size column's alias to 'Size', allowing it to be referenced by that name.
   * `set alias [filesystem | wards | watches | hashlogs] columnref, default 0/1;`
	   * Changes the given column's default status, causing it to appear in relevant queries that omit the columns clause.
		 * e.g `set filesystem Size, default 1;` will make the Size column in the filesystem a default return value for queries.
 * `reset out | standard | error | columnheaderset;`
   * Resets the targetting output stream to its default, thereby restoring it to the console.
   * Using `standard` resets the standard output, `error` the error, and `out` will reset both.
   * e.g `reset standard;` will reset the standard console output to its default destination.
 * `reset columnmappings;`
   * Recreates all of the column mappings, restoring them to their defaults.
	 * e.g `reset columnmappings;`

### Special Keywords
In HDSL, there are some special keywords that can be used universally, but only under specific circumstances.
 * The `force` keyword can be used prior to any path or bookmark anywhere a list of paths is valid (see syntax above).  When used, it will cause the statement to no longer validate the path's existence.  This allows, for example, explicit exclusion of files that do not yet exist but will in the future.
 * The ampersand `@`, when preceding a string, will make that string ignore escape sequences.  
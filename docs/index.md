{% include header.md %}

# Welcome to PhotoTags Synchronizer

Keep the tags where it belongs.

### Lot of functionality
![Ribbon Home](userguide/ribbon/ribbon_home.png)
![Ribbon Preview](userguide/ribbon/ribbon_preview.png)
![Ribbon Select](userguide/ribbon/ribbon_select.png)
![Ribbon Tools](userguide/ribbon/ribbon_tools.png)
![Ribbon View](userguide/ribbon/ribbon_view.png)

### Color themes
#### Blue themes
![Theme Lightmode](screenshots/theme_lightmode.png)
### Dark themes
![Theme Darkmode](screenshots/theme_darkmode.png)

## Userguide
[Userguide](userguide)

## Key features

- Keyword tagging
  - See user guide for [Keywords](userguide\keywords\)
  - Fast and easy editing meta information in a wide variety of media file formats.
  - [Keywords](userguide/keywords), [People and  Region](userguide/people), [GEOtagging](userguide/map) and [Dates](userguide/date)
  - Easy edit and tag many files at once
  - Copy and Paste from every Gridview into Clipboard and paste where you want (E.g. Microsoft Excel or Google Spreadsheets)
  - Unlimited Redo and Undo
  - Keep track of changes. Store full history of all changes for all meta information
- Synchronizer metadata
  - From Windows Live Photo Gallery
  - From Microsoft Photos
  - From Google Photo (where allowed)
  - Using a Powerful customizable web scraping tool in countries were allowed (PS: Use at your own risk, don't break the law).
- Powerful Exiftool GUI
  - Able to see all meta information provided by Exiftool
  - Able to see change history of meta information during updates
  - Compare meta information between files. Easily find changes done in meta information by comparing Files or previous change history in media files or between files and history previous saved information.
  - See more information in the user guide for [Exiftool tool GUI](userguide/exiftool)
  - See warning when tags mismatch, See more in user guide for [Warnings](userguide/warnings)
  - Can also write Microsoft Atoms back to files
- Powerful rename tool.
  - See user guide for [Rename tool](userguide/renametool)
- Powerful run command tool on a bulk of files
  - A helping tool for Convert, Update, Change and/or Update Photo and Video Files in Bulk using your favorite tool.
  - See user guide for [Run batch](userguide/runbatch)
- GEOtagging and Map
  - Import from Google History
  - Import from JSON and KML files
  - GEO tag using map
  - Lock up location name, region, city and country
- Chromecast
  - Support casting of video and pictures directly from PC with build-in webserver
- Media files support
  - Exif and metadata
    - Read and Write to around 200 [Media File formats using exiftool](https://exiftool.org/#supported)
  - Image formats:
    - Display and Chromecast 100 [Image File Formats supported by ImageMagick](https://imagemagick.org/script/formats.php)
  - Video formats:
    - Display and Chromecast over 30 types of [Video File Codecs using VLClib](https://wiki.videolan.org/VLC_Features_Formats/)
    - Convert around 200 [Video File Codecs using ffmpeg](https://www.ffmpeg.org/general.html#File-Formats)


## Key problems to solve
[More details](problems/)
- Don't lose your work and meta information.<br><br>When meta information are stored in cloud, your are not able to change provider without losing your tagging work.<br>When data is stored in local database, you will lose your tagging when change computer.<br><br>
  - Microsoft Windows Live Gallery
    - Store most of meta information in Media Files
    - Problem 1: But not all meta information will be saved, example, e.g. on many video files.
    - Problem 2: Save meta information using Microsoft Atoms not using international standards.
    - Problem 3: Many other tools, Exiftool can only read but can not save Microsoft Xtra Atoms
    - Problem 4: When moving media files from old computer to new computer, you lose meta information, because a lot of meta information is saved only in a local database and not in the media file.
  - Microsoft Photos
    - Problem 1: Store some information only in a local database and some information in the cloud.
    - Problem 2: There are no synchronization between data store locally and between other computers
    - Problem 3: In the past Longditude was truncated with zero's xx.12345 into xx.000000
    - Problem 4: When trying to change the coordinates on the media file, the metadata was changed correctly into the metadata in the file. However Microsoft Photos changes back to the old coordinates after uploading the photo to the clode and when sync back to the computer. 

  - Google Photos and most likely all other cloud storage providers
    - Problem 1: All data is stored in the cloud. If you want to move to another provider, all your tags are gone. According to GDPR this data is yours, but you are not able to download it.
  - Google Photos duplicatemedia files
    - Problem 1: WHen changing media files, Google Photos will do a new backup a file. Google Photos will keep the old version and a duplicate file be created.
    - See [Google Photos Duplicate Hack](google_photos_duplicate_hack.html)

## Keyword tagging

See and edit multiple meta information in multiple media files at once.

See what meta information has been saved about your media file in other applications. See what's been saved in the media file or what's only saved in the database or in the cloud.

See also User Guide for [Keywords](userguide/keywords)

![Keyword tagging](screenshots/screenshot_keyword_tags.png)

## Region name and people tagging

See and edit multiple regions like faces and people in multiple media files at once.

See what regions have been saved about your media file using other applications. See what's been saved in the media file or what's only saved in the database or in the cloud.

See also User Guide for [People](userguide/people)

![Region name and people tagging](screenshots/screenshot_people.png)

## Date and time

A set of useful tools that will help you find and set the correct date and time for your media file.

See also User Guide for [Date](userguide/date)

![Date and time tool](screenshots/screenshot_date_and_time.png)

## GEO tagging with Map, GPS tracker, Google Location History

A set of useful tools that will help you find and set correct GEOtagging for your media file.

You can see media files location on a map, and set a new location for your media file on the map.

You can find your media files location based on KML or json files with a GPS locations history / GPS tracking file.

See also User Guide for [Map](userguide/map)

![GEO tagging with Map](screenshots/screenshot_map.png)

## Powerful Exiftool GUI
- Show all meta information the ExifTool provides.
- Compare meta information between files
- Compare meta information before and after changes in the media file(s)


See also User Guide for [Exiftool GUI](userguide/exiftool)

![Exiftool GUI](screenshots/screenshot_exiftool.png)


## Give Warning when Tags mismatch

Show what fields are mismatched between different standards, when they should contain the same information.

See also User Guide for [Warnings](userguide/warnings)

![Give Warning when Tags mismatch](screenshots/screenshot_exiftoolwarnings.png)

## Windows Properties

Show and edit using Windows Properties

See also User Guide for [Properties](userguide/properties)

![Windows Properties](screenshots/screenshot_windowsproperties.png)

## Powerful Rename Tool

Rename files with information from the media file

See also User Guide for [Rename tool](userguide/renametool)

![Powerful Rename Tool](screenshots/screenshot_renametool.png)

## Convert and merge media files
- Combine multiple images and videos into a slideshow video
- Use the power of [ffmeg.exe](https://www.ffmpeg.org/) (or others tools) to convert videos

See also User Guide for [Convert & Merge](userguide/convert-and-merge)

![Convert and merge media files](screenshots/screenshot_convert_and_merge.png)

## Powerful tool for Convert, Update and to Change Photo and Video Files in Bulk

Easy start your favorite tool for selected media files and start your favorite tool for each media file with your arguments and use parameter variables from meta information values fetched from the media file.

See also User Guide for [Run batch](userguide/runbatch)

![](userguide/runbatch/runbatch-command-variables.png)

## Show and Chromecast videos and photos

- View images
- View videos
- Create Slideshow
- Chromecast picture
- Chromecast videos
- Create slideshow on Chromecast

See also User Guide for [Media preview and Chromecast](userguide\mediapreview-chromecast)

![Chromecast](screenshots/screenshot_preview_chromecast.png)

{% include footer.md %}
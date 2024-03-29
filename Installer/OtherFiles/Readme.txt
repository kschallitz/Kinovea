
Kinovea 0.8.9 - 2010/07/01.

Video analysis for coaches, athletes, medical professionals and everyone else too.


System Requirements:
--------------------
- CPU : 1 Ghz
- RAM : 256 MB
- Disk Space : 20 MB. 
- Screen Resolution : 1024x600 pixels.
- Software : Microsoft Windows (XP, Vista, 7) with .NET framework 2.0 or later.


License:
--------
Please see license.txt for details. In a nutshell:
- Source code : GPL v2.
- Graphics : CC-BY-SA.
- Manual : CC-BY-SA.
- Video Tutorials : CC-BY-SA. 


Communication channels:
--------
- Announcements: http://www.kinovea.org
- Forum: http://www.kinovea.org/en/forum/
- Mail: infos@kinovea.org
- Bugs reports: http://www.kinovea.org/bugs
- Demo Videos: http://kinovea.blip.tv/


Changelog:
----------
0.8.9 - July 1st 2010  - (r309) - Intermediate version, for testing.
    Added - Capture Screen - frame grabbing and recording.
    Added - Dual Export for images and videos - to create a composite image or video made from both videos.
    Added - Rich text edit for key images comments.
    Improved - Menus icons.
    Improved - Color picker with more colors and a list of recently choosen ones.
    Improved - Observational References - Support for sub directories, to be reflected as sub menus.
    Fixed - m210 - Time Marker Format won't save "Classic + Frame Numbers".
    Fixed - m207 - Comment box malfunction when attempting to save comments.
    
    
0.8.8 - June 1st 2010 - (r282) - Intermediate version, for testing only.

	Added - Observational References. SVG drawings as motion guides.
	Improved - Update to FFMpeg libraries. (in their r23012).
	Improved - Overview feature: implementation of scroll to refine number of images directly.
	Improved - Simplification of the upgrade manager.
	Improved - Global option for drawings persistence to be always visible by default.
	Improved - Possibility to set the default persistence to 1 frame.
	Improved - Speed percentage now uses the action timeframe instead of the video timeframe (high-speed cameras).
	Improved - Spreadsheet export support for Key image time, Lines measures and angles.
	Improved - Added .f4v, .mts and .gif to the list of known file formats for the file explorer.
	Fixed - m195 - Seek issues with ASF file.
	Fixed - m194 - Basler AVI file can't be opened. (new FFMpeg)
	Fixed - m192 - Speed measurement does not reflect high-speed camera settings. 
	Fixed - m189 - MOV with watermark can't be read. (new FFMpeg)
	Fixed - m188 - Trajectory label is not exported on ODF files.
	Fixed - m185 - Magnifier does not magnify the right part of the image when mirroring.
	Fixed - m184 - WMV from Windows Movie Maker diaporama can't be read. (new FFMpeg) 
	Fixed - m165 - WMV files playing incorrectly. (new FFMpeg)
	Fixed - m164 - Magnifier is not exported on video saving.


0.8.7 - May 7th 2010 - (r257) - Stable version.

	Improved - Updates to Norwegian, Finnish, Dutch, Turkish, Romanian, German, Italian, Greek, Chinese.
	Improved - Video save operations are now cancellable.
	Fixed - m187 - Preferred speed unit is not initially selected when reopening.

0.8.6 - April 3rd 2010 (r235) - Release candidate version.

	Improved - Updates to Turkish and Italian.
	Improved - During synchronization, speed change in one video is automatically reported on the other.
	Improved - During synchronization, actions causing a pause on one video also cause one on the other.
	Improved - Automatic tracking is improved both in speed and robustness.
	Fixed - m179 - Synchronisation is lost when one video is forced to slow down.
	Fixed - m178 - Repeat mode is wrong when loading another video in the same screen.
	Fixed - m177 - Keyboard shortcuts not working when file open through command line.
	Fixed - m176 - Key images not visible when saved within a working zone.
	Fixed - m175 - Crash while opening a video.
	Fixed - locale was forced to French when no user preferences, instead of the system locale or English if not supported.

0.8.5 - February 28th 2010 (r211) - Intermediate version, for testing only.

	Added - Localizations : Finnish, Norwegian, Turkish, Greek and updates to Italian, Spanish, Portuguese, Dutch, German, Romanian. 
	Added - Distance and Speed display option on Tracks.
	Added - Export Tracks trajectory data to text.
	Improved - new menu to directly track a point from general right click, out of any Cross marker context. 
	Improved - menu to quickly access the time code format options.
	Improved - Markers in the frame tracker gutter to see stopwatches and paths.
	Improved - Measure label on lines can now be moved around.
	Fixed - m170 : Double click using the Pencil tool causes the configuration dialog to appear.
	Fixed - m169 : Saving ends up with an error message, even though the saving was ok. 
	Fixed - m167 : Drag and drop of a file on the Kinovea.exe file or on a shortcut does not load the video. 
	Fixed - m166 : Mirror filter was not carried over during sync superposition. 
	Fixed - Key image markers were not updated in Frame Tracker when removing a key image.
	Fixed - Empty player crashed in 2 screens mode when using keyboard shortcuts with impact on both screens. 
	Fixed - Added .TOD extension to known video file formats. 

0.8.4 - November 24th 2009 (r153) - Intermediate version, for testing only.

	Added - Option to superpose images from each other video during synchronisation.
	Added - Configuration window to set origin of coordinate system for Paths.
	Added - Image files show up in the Explorer and in the Thumbnails Explorer.
	Fixed - crash on video loading for some systems.
	Fixed - sometimes a screen became unstoppable during synchronisation. 
	Fixed - bug that sometimes missed to alert the user when closing screen even if he had added Metadata.
	Fixed - crash when adding a drawing on image file opened as video.
	Fixed - bug on StopWatches where the background did not scale up when image size was increased.
	

0.8.3 - November 11th 2009 (r143) - Intermediate version, for testing only.

	Added - Options in general Preferences to open files forcing image aspect ratio and deinterlacing.
	Fixed - m145 - Crash at startup for Norvegian based systems.
	Fixed - m147 - Memory leak on deinterlace.
	 

0.8.2 - November 1st 2009 (r139) - Intermediate version, for testing only.

	Added - Trajectories (Paths) - new "focused" mode where the trajectory is only visible around the current frame.
	Added - Command line arguments handler.
	Added - Possibility to force the aspect ratio to 16:9 or 4:3 if autodetect is wrong. 
	Improved - Tracing of unhandled exceptions in an external file.
	Improved - Cancel button on the "extraction of frames to memory" progress bar. 
	Improved - Usability and performance enhancements on common frame tracker during synchronisation.
	Fixed - m140 - Exported coordinate system is wrong (on Y) for trajectories.
	Fixed - m144 - In Dynamic sync, we were using absolute positions instead of positions relative to the working zone. 
	Fixed - When we changed key image title through direct edit, the trajectory KeyframesLabels weren't updated. 
	

0.8.1 - August 9th 2009 - (r117) - Intermediate version, for testing only.

	Added - Export - "Paused Video" saving method. Video with longer pauses on Key Images. 
	Improved - Export - Simplified the saving dialog.
	Improved - Export - Export to Spreadsheets (ODF, Excel, XHTML) now uses user units (time and length).
	Fixed - m135 - Uncompressed files from VirtualDub cannot be saved again in Kinovea.
	Fixed - m137 - Error on Vista when opening MPG or MOD files.
	Fixed - Selection wasn't imported to memory when using the in/out buttons. 


0.8.0 - July 8th 2009 - (r104) - Intermediate version, for testing only.

	Added - Explorer - Shortcuts tab.
	Added - Motion filters - Mosaic mode.
	Added - Motion filters - Reverse selection (backward playback).
	Added - Export - Export to OpenOffice calc, MS-Excel, XHTML.
	Improved - Explorer - Possibility to rename and delete files directly.
	Improved - Explorer - Thumbnails loop between several images from the video.
	Improved - Explorer - Thumbnails keep image ratio and are centered.
	Improved - Playback - CTRL + Up / Down changes speeds by 25% increments.
	Improved - Playback - New timecode format : classic time + frame number combination.
	Improved - Key Images - Default key image title (timecode) is updated live until the user choose an explicit title.
	Improved - Key Images - Possibility to change key image title directly without going through the comment window.
	Improved - Key images - The key images panel stays docked if the user explicitely asked so.
	Improved - Drawings - Line length display and seal option.
	Improved - Chronos - Countdown mode.
	Improved - Preferences - Thumbnail size, explorer tab and splitters positions are saved to prefs.
	Fixed - m133 - Crash in explorer when "Asus EEE Storage" application is installed.
	Fixed - Explorer - Collapsing a node when a subnode was expanded resulted in automatic re-expanding.
	Fixed - Explorer - Scrolling while thumbnails were loading caused error in the thumbnails positions. 
	Fixed - Saving - When saving non analysis mode videos a memory leak could make the RAM peak and computer hang. 
	

0.7.10 - February 4th 2009 - (r55) - Stable version.

	Fixed - Selection and saving issues on files with B-frames.

0.7.9 - January 23th 2009 - (r39) - Intermediate version, for testing only.

	Added - Logging system to improve defect fixing.
	Added - Italian user guide.
	Added - Undo emulation for Image adjustment menus.
	Improved - Splash screen now also covers initial language load.
	Improved - When undoing a 'close screen' command, all its key images and drawings are revived.
	Improved - Non square pixels videos are now handled in display and save.
	Improved - Trajectory now have rounded angles.
	Improved - Manipulation handles zones increased in size.
	Fixed - m0109 - A bug could make the application crash at Player initialization.
	Fixed - m0111 - Measured angles show permanently instead of the assigned number of frames
	Fixed - m0108 - Saving a trajectory muxed in the video file could behave improperly.
	Fixed - Status bar update on file list click.
	Fixed - Configuration dialog box are relocated to center of screen if current mouse location make them go outside screen.


0.7.8 - December 18th 2008 - (r24) - Intermediate version, for testing only.

	Added - Direct Zoom.
	Added - Trajectories suggestions with template matching.
	Added - Label Follows and Arrow Follows mode for trajectories.
	Added - Specification of the capture fps for high speed cameras.
	Improved - Video size is not forced to be multiple of 4.
	Improved - Drawings can not be moved outside screen anymore.
	Improved - Shift + Left arrow on first image moves backwards to the end.
	Improved - Explorer configuration is now saved in settings.
	Improved - Hand and cross custom cursor.
	Improved - Warning dialog box if key images data not saved.
	Improved - F11 toggles stretch mode.
	Improved - Keyboard navigation (TAB) on most dialogs. 
	Fixed - Launch issue on some configs.
	Fixed - ColorProfile.xml removed on uninstall.
	Fixed - Closing configuration windows with the red cross does a Cancel.
	Removed - PDF Export.


0.7.7 - November 24th 2008 - Intermediate version, for testing only.

	Added - Persistence of drawings. ( =Fading in/out) + Related configurations dialogs + "Go to key image" menu.
	Added - Drawings color & style preconfiguration window. (=right click before drawing) 
	Added - Markers for key frames in the navigation bar.
	Improved - Pencil tool is kept active when changing frames. + Escape returns to the hand tool.
	Improved - Pencil line now has rounded caps. 
	Improved - Pencil tool cursor is now a colored circle.
	Improved - Pencil tool style picker has more size options. 
	Improved - Pencil width and font size (chronos and texts) now scales with image.
	Improved - Grids, chronos and Trajectories now exported on video and diaporama.
	Improved - Configurations dialogs now opens at mouse location instead of center of screen.
	Fixed - Trajectory color is now taken from its parent cross maker, not from default cross marker color.

0.7.6 - November 11th 2008 - Stable version.

	Added - Localizations : German, Portuguese, Polish, Spanish.
	Added - File thumbnails on right pane.
	Added - Trajectory tool (Manual tracking).
	Added - Chronometer tool.
	Added - Magnifier mode.
	Added - Width of line and pencil tool, arrows endings for lines, font size for text.
	Added - Reimplemented the 3D Grid with Homography Matrix. Now called 'Perspective Grid'.	
	Added - Deinterlace option to remove comb artifacts.
	Added - Changing Drawings color / style after setup.
	Added - Image export is now at current display size.
	Fixed - Exported single image takes time format into account.
	Fixed - default bitrate increased if not conclusive.
	Fixed - Crash hapenned when mouse moved while placing a chronometer.
	Fixed - Diaporama Export.
	Fixed - Some FLV made the whole app crash. 
	Fixed - Ergonomics timeformats now have 3 significant digits.
	Fixed - If the video has several streams we take the one with most frames in it.
	Fixed - Crash on zero sized Angles.
	Fixed - Crash when adding a drawing while playing.
	Fixed - Crash when closing a screen while synchronizing two videos.
	Fixed - Crash when openning a blank screen after having synchronized two videos.
	Fixed - .mod files were not recognised in file explorer.
	Fixed - Drawing moved to a different location.
	Fixed - File Explorer now starts at Desktop level.

0.7.2 - July 14th 2008 - Stable version.

	Added - Key Images (Add, browse, comments, etc.)
	Added - Drawings on Key Images (Line, angle, text, etc.)
	Added - Export / Import Key Images data between videos.
	Added - Save Working Zone as new video file.
	Added - User Preferences.
	Added - 3D Plane. Grids interactivity.
	Added - Various export options for images.
	Added - Mousewheel to browse video.
	Added - Ability to change time markers.
	Added - Dutch locale.
	Fixed - Dynamic Synchronization.
	Fixed - Windows Vista 64 Bits. 

0.6.3 - March 12th 2008 - Hotfix release.

	Added - Display of ChangeLog within the Update Dialog box. 
	Fixed - Bug when running Windows Vista without admin rights.
	Fixed - Kinovea logo is now under LAL (Licence Art Libre) license.

0.6.2 - March 08th 2008.
	First public release.

# .NET Japanese Crossword Generator
A .NET program that generates "Japanese Crosswords" from an input image.

Features:  
* Currently only supports black/white colors, uses a YIQ algorithm to convert every pixel to either black or white depending on set contrast and RGB values.
* Supports flipping between foreground/background black
* Automatically sets 0 alpha pixels to either black or white
* Supports input image scaling using bicubic interpolation
* Supports crosswords up to 200x200 squares large (anything larger would take too much RAM 200x200x20x24 = 2,3GB)
* Can take any GDI+ supported image type: BMP, GIF, EXIF, JPG, PNG and TIFF
* Uses WPF, .NET is the only dependency
* Standalone EXE, no install, no resources

# EFT DMA Radar
This is a work in progress and has not been optimized/finished. There are a few known issues and this isn't 100% stable. Crashes *can* occur.

[UC Forum Thread](https://www.unknowncheats.me/forum/escape-from-tarkov/482418-2d-map-dma-radar-wip.html)

### Instructions
1. You need a DMA Device (Screamer, Raptor DMA,etc.) installed on your game PC with (hopefully) good/safe firmware. Don't ask me how. A memory map may also be desired (name it mmap.txt and put in the .exe directory).
2. Build/compile the app for Release x64.
3. Import any maps you would like to use (.PNG files only) into the \Maps sub-folder. Make sure you have a corresponding .JSON file with the same name. For example, `Customs.PNG` and `Customs.JSON`
4. Run the program on your 2nd PC (NOT GAME PC!!!) that has the DMA USB Cable plugged into. Click the Map button to cycle through maps if you need.
5. Make sure you have "Automatic RAM Cleaner" turned OFF in your game settings.

### Map JSON Info
Format your JSON as below. The X,Y values are the pixel coordinates on your .PNG image at game location 0,0,0 (this can be found on the right hand side of the window). You may need to play with the scale value to find the right setting depending on your map.

I used the below JSON Values for the Customs map in the screenshot below:
```
Customs.JSON
{
	"x": 1292.0,
	"y": 996.0,
	"z": 0.0,
	"scale": 3.75
}
```

### Demo
![Demo](https://user-images.githubusercontent.com/42287509/148650370-8ab172e4-f303-4897-96ca-b690afa897b2.png)

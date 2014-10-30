# ScreenLapse
A simple console application for capturing your screen by a certain interval
for creation of time lapses.

## Configuration format
- Include and exclude are regular expressions for process names.
- Output path and interval must be specified
- If only width or only height is specified, the other is automatically calculated.

```json
{
    "output": "img/cap_{0000}",
    "include": [
        "explorer",
        "sublime"
    ],
    "exclude": [
        "chrome"
    ],
    "width": 1920,
    "height": 1080,
    "interval": 2000
}
```

## Creating a timelapse
```
screenlapse ./mytimelapse.json
ffmpeg -f image2 -i ./cap_%05d.jpg -c:v mpeg4 -pix_fmt yuv420p -qscale 0 -r 8 ./mytimelapse.mp4
```

## TODO
- Allow selection of screen and region.

# randvis
crappy little editor/visualizer for randomizer configuration.

TODO: implement this into mod

## requirements
- [LOVE](https://love2d.org/)

## setup
1. install cimgui-love library
```bash
git clone https://codeberg.org/apicici/cimgui-love tmpclone
mv tmpclone/cimgui .
rm -rf tmpclone
```
2. install cimgui-love binary from https://codeberg.org/apicici/cimgui-love/releases
3. extract uiSprites.json and uiSprites.png from rain world assets, and place them into this directory.
4. download ProggyClean.ttf from [here](https://github.com/bluescan/proggyfonts/raw/refs/heads/master/ProggyOriginal/ProggyClean.ttf)

## run
```bash
# assuming cwd is rw waves repository
love randvis assets/wavedata/randomconfig.json
```
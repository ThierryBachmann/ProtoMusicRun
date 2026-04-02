# ProtoMusicRun - Maestro MIDI Rhythm Demo  
*A rhythm game built with Unity and powered by the Maestro MIDI Player Tool Kit.*

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Unity 6](https://img.shields.io/badge/Unity-6.0-black?logo=unity)
[![Open Source](https://img.shields.io/badge/Open%20Source-GitHub-blue?logo=github)](https://github.com/)

---

## Screenshots  
A quick look at the demo in action:

<h3 align="center">Level 2</h3>
<p align="center">
  <img src="Screenshots/Level_2.png" width="600">
  <br>
  <em>Main Level — Gameplay</em>
</p>

<p align="center">
  <img src="Screenshots/Level_2-1.png" width="600">
  <br>
  <em>Level 2 — Variant</em>
</p>

<h3 align="center">Some Information</h3>
<p align="center">
  <img src="Screenshots/Setting.png" width="500">
  <br>
  <em>Settings Menu: Lite mode and Firebase Leaderboards name</em>
</p>

<p align="center">
  <img src="Screenshots/what.png" width="500">
  <br>
  <em>Additional UI Screen</em>
</p>

<h3 align="center">Terrain Designer</h3>
<p align="center">
  <img src="Screenshots/Terrain_designer_1.png" width="600">
  <br>
  <em>Terrain Designer — View 1 - with MIDI settings</em>
</p>

<p align="center">
  <img src="Screenshots/Terrain_designer_2.png" width="600">
  <br>
  <em>Terrain Designer — View 2 - Vegetation and Terrain</em>
</p>

<p align="center">
  <img src="Screenshots/Terrain_designer_3.png" width="600">
  <br>
  <em>Terrain Designer — View 3 - Bonus & Instrument</em>
</p>

---

## Story

A legendary music group **Animals Disturbance**, famous across the galaxy, travels from planet to planet to perform their concerts.
After landing on a remote and arid world, disaster strikes: all of their instruments mysteriously vanish.

While the band begins their rehearsal — not an easy task without instruments — you, a member of the technical crew, rush out to recover everything before the session ends.

There is just one problem.
Right before landing, you drank a cup of TurboCafé-7, a hyper-caffeinated alien beverage known for its “You won’t stop. Literally.” effect.
Now your body keeps accelerating on its own, and the only way to slow down is to crash into vegetation, rocks, or anything solid enough to absorb the momentum.

Find the instruments, control your speed the hard way, and save the concert!

---

## Purpose of this Demo  
This project demonstrates how **MIDI data can shape gameplay** — and how gameplay can dynamically influence the music.  
It showcases:  
- Dynamic **tempo changes** related to the player speed.  
- **Realtime Instrument swaps**.  
- **Sound cues** with pitch shifts effects for bonus.
- **Pitch-bend cues** efffect when player collide ab obstacle.
- **Spatial audio** features is helping searching the goal. 

**It's a Demo**
- Minimal visuals, simple terrain generator quite repetitive ;-) and a few example levels to get you started.
- Always in progress, for example score calculation need to be reviewed.
- Code comments are also to be improved.
- WebGL compliant — but for the best sound, build for desktop or smartphone. WebGL is not good for sound generation.
- Works partially on mobile with a browser ... but FPS very deceptive although a low level of visuals.
- Any advice to help improve mobile browser performance is very welcome!

---

## Gameplay & Features  
Beyond the musical system, this demo includes several essential components of a functional game:  
- New input system with gamepad support  
- Touchscreen-friendly controls  
- Terrain editor based on chunks for infinite world creation  
- Algorithmic terrain generator (POC level, not yet used in the demo!)  
- Integration with Firebase Leaderboards (Hall of Fame)  
- Shader exploration for small effects  
- Example levels built with simple terrain and minimal visuals

---

## Game Input  
The demo supports multiple input methods so you can play almost anywhere:

### Touchscreen
- **Tap** → jump
- **Swipe** → turn

### Gamepad
- **Start** → Start the run  
- **A** → Jump  
- **Left Stick** → Turn left or right  

### Keyboard
- **Enter** → Start the run  
- **Space** → Jump  
- **Right / Left Arrow** *or* **A / D** → Turn  

---

## About This Repository  
This project is provided as a **complete, open-source Unity demo** to help developers understand how to combine:  
- Unity 6  
- Maestro MIDI Player Tool Kit
- Gameplay logic driven by MIDI  
- Procedural content  
- Online leaderboards, to resuse create your account with your secret key.  

**Source code included — customize it and make it your own!**

---

## Important Links  
- Free version: [Maestro – Midi Player Tool Kit – Free](https://assetstore.unity.com/packages/tools/audio/maestro-midi-player-tool-kit-free-107994)  
- Pro version: [Maestro – Midi Player Tool Kit – Pro](https://assetstore.unity.com/packages/tools/audio/maestro-midi-player-tool-kit-pro-115331)  

> *Note: The demo uses the Free version by default. If you own the Pro version, you can easily integrate its advanced features.*

---

## Getting Started  
1. Clone or download this repository  
2. Open the project in **Unity 6**  
4. Import the Maestro package (Free or Pro) from the Asset Store, **minimum version 1.9.0**
5. Open the scene MusicBabyDemo/Scenes/MusicRunBabyDemo
6. Press **Play** and explore the demo levels  
7. Modify, extend, and experiment freely - And, Use a good stereo audio setup!

---

## Requirements  
- Unity 6  
- Maestro MIDI Player Tool Kit (Free or Pro)  
- Optional: Firebase setup for leaderboard posting  
- A good stereo audio setup for the best experience  

---

## 🛠️ Project Structure  
 - in progress

---

## Contributing  
Contributions are welcome!  
Feel free to open issues, submit pull requests, or propose improvements.

---

## License  
This project is licensed under the **MIT License**.  
See the file [LICENSE](LICENSE) for more details.

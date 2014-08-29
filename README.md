# [S.A.R.A.H.](http://sarah.encausse.net)

[S.A.R.A.H.](http://sarah.encausse.net) is an OpenSource client/server framework to control Internet of Things using Voice, Gesture, Face, QRCode recognition. It is heavily bound to Kinect v1 and v2 SDK.


This project contains C# **Client for SARAH**. And will communicate with [NodeJS Server for SARAH](...).


## License

```
            DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE
                    Version 2, December 2004

 Copyright (C) 2012 S.A.R.A.H. <sarah.project@encausse.net>

 Everyone is permitted to copy and distribute verbatim or modified
 copies of this license document, and changing it is allowed as long
 as the name is changed.

            DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. You just DO WHAT THE FUCK YOU WANT TO.
```

```
 This program is free software. It comes without any warranty, to
 the extent permitted by applicable law. You can redistribute it
 and/or modify it under the terms of the Do What The Fuck You Want
 To Public License, Version 2, as published by S.A.R.A.H. See
 http://www.wtfpl.net/ for more details.
```

**Be Warned: some dependencies may require fees for commercial uses. Kinect XBox 360 can ONLY be used for development purpose**

## Description

SARAH v4.0 Client is built on an **AddOns Architecture** sharing common properties. Each AddOn:

- communicate with each other using AddOnManager
- MUST implements IAddOn or AbstractAddOn
- MUST be declared in properties and can be prevent from starting with `{name}.enable` property
- Use Tasks to work with stream asynchronously and tune CPU usage.

Client's Addons are stored in *AddOns* folder and have **no relation with SARAH plugins**. The idea is to sandbox features and library into subprojects. 


This page describe Client's Core/AddOns specification. SARAH developers should see :
- [SARAH Wiki](http://wiki.sarah.encausse.net)
- [SARAH RFE and Issues](http://issues.sarah.encausse.net)


## AddOns: Blob

UNDER CONSTRUCTION

This AddOn use EmguCV to Detect and Draw blobs in camera frame according to color, shape, features, ...


## AddOns: Body Engine

SARAH provides NBody: an abstract representation of Body / Skeleton implemented by Kinect 1, Kinect 2, and other computer vision tools.

This AddOn draw body parts, joints and relevant areas.

- HTTP & XML `gesture` parameter to start/stop recognition

## AddOns: Debug

This AddOn implements logging features to output relevant data about SARAH:

- All logs in a log file
- Audio (wav) of matching commands
- Audio (wav) of SARAH voice speech

### Properties

```
; Path to log file
log-file=AddOns/debug/${shortdate}.log

; Path to recognized command
dump=AddOns/debug/dump/${date}.wav

; Path to audio response
voice=AddOns/debug/voice/${date}.wav"
```

## AddOns: Face Recognition

This AddOn use EmguCV to perform Face Detection and Face Recognition. In a Nutshell:
 
- Provide a GUI to train new faces
- Draw detected faces' rectangle
- Update NBody data with face rectangle
- Update NBody data with face name
- Forward recognition to ProfileManagers
- Store datas in AddOn's directory
- HTTP & XML `face` parameter to start/stop recognition 

### Properties

Choose face detection and recognition algorithms. See also [CodeProject article](http://www.codeproject.com/Articles/261550/EMGU-Multiple-Face-Recognition-using-PCA-and-Paral).

```
; Cascade for Face detection
HaarCascade=AddOns/face/haarcascades/haarcascade_frontalface_default.xml

; Algorithm confidence
Eigen_Threshold=0
Fisher_Threshold=0
LBP_Threshold=30
```

## AddOns: Google Speech

This AddOn use GoogleSpeech API to perform Speech2Text on speech recognitions.

- Process grammar with *dictation* attribute
- Process rejected dynamic grammar

Since 2014 Google narrow it API v2 to 50 req/day ! **An API Key MUST be provided** to the plugin (See G+ tutorial and explanations).

## AddOns: HTTP Engine

This plugin handle HTTP communication with SARAH Server. It provides to other AddOns the ability to send GET/POST/FILE request with a custom token.

- Send request for SpeechRecognition
- Send requestion for MotionDetection
- All request are filled with contextual data (ClientId, Profile, ...)
- BODY response is forwarded with the token to other AddOns 


### Properties

```
[http.remote]

; Remote address of NodeJS server
server=http://127.0.0.1:8080

[http.local]

; Local HTTP server port
port=8888

temp=AddOns/http/temp/
```

### FIXME
- Start HTTP Server on 8888

## AddOns: IP Camera

UNDER CONSTRUCTION

This AddOn use EmguCV or nVLC to process frames of IP Camera 
- but EmguCV hangs forever...
- but nVLC Crash ...


## AddOns: Kinect v1

This AddOn use Kinect SDK v1.8  to start all connected Kinects v1 XBox360 or Windows.

- Start a SpeechEngine with Kinect audio stream with a given confidence
- Start a color stream of 1280 x 960 at 12 fps
- Start a skeleton stream for other AddOns
- Detect motion using depth stream 
- Provides sub plugin framework

The motion detection stops as much as possible stream when there is no motion. Properties allow control of streams: Audio stream only or custom fps.

### FIXME

- Handle Gesture
- Handle Face

## AddOns: Kinect v2

This AddOn use Kinect SDK v2.0  to start one connected Kinects v2 for Windows.

- Start a SpeechEngine with Kinect audio stream with a given confidence
- Start a color stream of 1920 x 1080 at 15/30 fps
- Start a skeleton stream for other AddOns
- Detect motion using depth stream 
- Provides sub plugin framework

The motion detection stops as much as possible stream when there is no motion. Properties allow audio stream only.

### FIXME

- Handle Gesture
- Handle Face

## AddOns: Microphone

This AddOn use Microphone to start a SpeechEngine with given confidence.

### Properties

```
; Confidence of recognition (from 0 to 1)
confidence=0.7

; Define the audio device index (0 is the default device, see logs)
device=0
```

### FIXME

- Handle multiple device number

## AddOns: OS Manager

This AddOn handle all OS specific actions
 
- Watch specific folder to perform speech recognition
- HTTP & XML `run` and `runp` parameter to run a process
- HTTP & XML `activate` parameter to foreground a process
- HTTP & XML `keyText` parameter to simulate key press
- HTTP & XML `keyUp` parameter to simulate key up
- HTTP & XML `keyDown` parameter to simulate key down
- HTTP & XML `keyPress` parameter to simulate key press
- HTTP & XML `recognize` parameter to regognize path 

## AddOns: Pitch Tracker

This AddOn use the speech audio stream to compute voice pitch and update ProfileManagers. 

## AddOns: Profile Engine

This AddOn manage profile for people playing with SARAH.

- A profile is cross device
- Face is a major profile key
- Handle engagement with SARAH

### FIXME

- Handle people engagement
- Add profile timeout on properties
- Store profile to disk
- Forward profile to HTTP Request
- Display profile in GUI

## AddOns: Prominent Color

This AddOn compute color frame of each device to draw most prominent color.

### FIXME

- Do nothing if device window is not displayed OR forward color to ProfileManager (is it relevant ?)


## AddOns: QRCode

UNDER CONSTRUCTION


## AddOns: RTP Client

This AddOn start an RTP Client on a given Thread to listen to inbound stream

Can be called on RaspberryPi using ffmpeg
```
ffmpeg -ac 1 -f alsa -i hw:1,0 -ar 16000 -acodec pcm_s16le -f rtp rtp://192.168.0.8:7887
avconv -f alsa -ac 1 -i hw:0,0 -acodec mp2 -b 64k -f rtp rtp://{IP of your laptop}:1234
```

Or using Kinect on windows
```
ffmpeg  -f dshow  -i audio="Réseau de microphones (Kinect U"   -ar 16000 -acodec pcm_s16le -f rtp rtp://127.0.0.1:7887
```

### Properties

```
; Confidence of recognition (from 0 to 1)
confidence=0.6

; Define the RTP Port
port=7887
```

## AddOns: Speaker Engine

This AddOn manage speaker to play audio stream:

- List available speakers
- Handle voice strream
- Play stream asynchronously or not
- HTTP `speaking` parameter write "speaking" if client currently speaking
- HTTP & XML `notts` parameter stop speaking
- HTTP & XML `stop` parameter stop playing audio
- HTTP & XML `play` parameter play given audio

### Properties

```
; The device index to play on (use -1 for default device)
device=-1

; Volume level of device
volume=50

; Timeout in seconds to stop the player
timeout=480
```

### FIXME

- Play other streal (music, song, ...)
- Allow custom stream timeout


## AddOns: Speech Engine

This AddOn build a Speech Engine for each provided audio stream (Kinect, Microphone, ...).
 
- Grammar Manager maintains a cache of XML of plugins
- Grammar can be in multiple languages 
- SARAH name is hot replaced according to properties
- Context Manager run/stop grammars accortding to context
- Context Manager handle dynamic grammar
- The `bot.name` must match an improved confidence
- `bot.name` is optional with user is engaged
- Good match trigger engagement until a given timeout
- HTTP & XML `listen` parameter start/stop listening
- HTTP & XML `context` parameter set the context
- HTTP `grammar` parameter set the XML of a grammar
- HTTP `sentences/tags` parameter for AskMe
- HTTP & XML `asknext` parameter to call tts on a rule

### FIXME

- Context should apply to a given device instead of all. But it's really complicated.
- Improve behavior for optional "SARAH" if confidence is strong and for a given timeout.

## AddOns: Voice Engine

This AddOn perform text to speech according to given actions:

- List available voices
- Text2Speech HTTP Body with token "speech"
- Text2Speech HTTP & XML `tts` parameter

### FIXME

- Select custom voice at a given time


## AddOns: WebSocket

UNDER CONSTRUCTION

## AddOns: Window Manager

This AddOn provide a window (and a menu item) to display other AddOns data.

- Window are sized according to color frame (ratio)
- Window have a sidebar where addons put new controls
- A dedicated footer can be used by ProfileManager
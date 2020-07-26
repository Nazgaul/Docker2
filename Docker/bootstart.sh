#!/bin/bash

echo "$PAD Create dummy audio and video devices."
Xvfb :99 -screen 0 1920x1080x24 -nolisten tcp &
echo "$PAD Created dummy devices."
#!/bin/bash
# Preferences and feature toggles — app-managed state (onboarding,
# update checks, window sizes) is left alone.
# The running app picks these up on its next launch.

set -euo pipefail

# Feature toggles
defaults write com.vorssaint.utils "featureAvailable.appUpdates" -bool true
defaults write com.vorssaint.utils "featureAvailable.autoQuit" -bool true
defaults write com.vorssaint.utils "featureAvailable.brightness" -bool false
defaults write com.vorssaint.utils "featureAvailable.cameraPreview" -bool false
defaults write com.vorssaint.utils "featureAvailable.cleaner" -bool false
defaults write com.vorssaint.utils "featureAvailable.cleaningMode" -bool true
defaults write com.vorssaint.utils "featureAvailable.clipboardHistory" -bool false
defaults write com.vorssaint.utils "featureAvailable.colorPicker" -bool true
defaults write com.vorssaint.utils "featureAvailable.commandBar" -bool false
defaults write com.vorssaint.utils "featureAvailable.diskImageInstaller" -bool false
defaults write com.vorssaint.utils "featureAvailable.dockClick" -bool false
defaults write com.vorssaint.utils "featureAvailable.dockPreview" -bool false
defaults write com.vorssaint.utils "featureAvailable.extraBrightness" -bool false
defaults write com.vorssaint.utils "featureAvailable.fanControl" -bool false
defaults write com.vorssaint.utils "featureAvailable.finderCutPaste" -bool false
defaults write com.vorssaint.utils "featureAvailable.finderRename" -bool false
defaults write com.vorssaint.utils "featureAvailable.focusFollowsMouse" -bool false
defaults write com.vorssaint.utils "featureAvailable.homebrew" -bool true
defaults write com.vorssaint.utils "featureAvailable.keepAwake" -bool true
defaults write com.vorssaint.utils "featureAvailable.keyboardDebounce" -bool false
defaults write com.vorssaint.utils "featureAvailable.mediaTools" -bool false
defaults write com.vorssaint.utils "featureAvailable.micMute" -bool false
defaults write com.vorssaint.utils "featureAvailable.middleClick" -bool true
defaults write com.vorssaint.utils "featureAvailable.mixer" -bool true
defaults write com.vorssaint.utils "featureAvailable.monitorCPU" -bool true
defaults write com.vorssaint.utils "featureAvailable.monitorDisk" -bool true
defaults write com.vorssaint.utils "featureAvailable.monitorGPU" -bool true
defaults write com.vorssaint.utils "featureAvailable.monitorMemory" -bool true
defaults write com.vorssaint.utils "featureAvailable.monitorNetwork" -bool true
defaults write com.vorssaint.utils "featureAvailable.monitorPower" -bool true
defaults write com.vorssaint.utils "featureAvailable.mouseButtonShortcuts" -bool false
defaults write com.vorssaint.utils "featureAvailable.mouseNavigation" -bool false
defaults write com.vorssaint.utils "featureAvailable.musicBlock" -bool false
defaults write com.vorssaint.utils "featureAvailable.pastePlain" -bool false
defaults write com.vorssaint.utils "featureAvailable.quickLauncher" -bool false
defaults write com.vorssaint.utils "featureAvailable.quickToggles" -bool false
defaults write com.vorssaint.utils "featureAvailable.radialMenu" -bool false
defaults write com.vorssaint.utils "featureAvailable.scratchpad" -bool false
defaults write com.vorssaint.utils "featureAvailable.screenOCR" -bool false
defaults write com.vorssaint.utils "featureAvailable.screenRecorder" -bool false
defaults write com.vorssaint.utils "featureAvailable.screenshot" -bool true
defaults write com.vorssaint.utils "featureAvailable.scrollInverter" -bool true
defaults write com.vorssaint.utils "featureAvailable.shelf" -bool true
defaults write com.vorssaint.utils "featureAvailable.smoothScroll" -bool true
defaults write com.vorssaint.utils "featureAvailable.soundOutputSwitcher" -bool false
defaults write com.vorssaint.utils "featureAvailable.superKey" -bool false
defaults write com.vorssaint.utils "featureAvailable.switcher" -bool false
defaults write com.vorssaint.utils "featureAvailable.textSnippets" -bool false
defaults write com.vorssaint.utils "featureAvailable.uninstaller" -bool false
defaults write com.vorssaint.utils "featureAvailable.urlCleaner" -bool false
defaults write com.vorssaint.utils "featureAvailable.windowLayout" -bool true
defaults write com.vorssaint.utils "featureAvailable.windowMaximizer" -bool false

# Keep-awake
defaults write com.vorssaint.utils autoQuitEnabled -bool true
defaults write com.vorssaint.utils batteryLimitPercent -int 10
defaults write com.vorssaint.utils defaultDurationMinutes -int 60
defaults write com.vorssaint.utils keepAwakeActiveIcon -string vorssaint
defaults write com.vorssaint.utils keepAwakeExternalDisplay -bool false
defaults write com.vorssaint.utils keepAwakeIconTint -string orange
defaults write com.vorssaint.utils keepAwakeMouseJiggleIntervalMinutes -int 5
defaults write com.vorssaint.utils showCountdownInMenuBar -bool true

# System monitor
defaults write com.vorssaint.utils menuBarMetricAppearance -string values
defaults write com.vorssaint.utils monitorAlertBatteryPercent -int 15
defaults write com.vorssaint.utils monitorAlertCPUTemperatureThreshold -int 90
defaults write com.vorssaint.utils monitorAlertCPUThreshold -int 90
defaults write com.vorssaint.utils monitorAlertCooldownMinutes -int 15
defaults write com.vorssaint.utils monitorAlertDiskFreePercent -int 10
defaults write com.vorssaint.utils monitorIntervalSeconds -int 2
defaults write com.vorssaint.utils monitorMemoryMetric -string used

# Input & windows
defaults write com.vorssaint.utils middleClickEnabled -bool true
defaults write com.vorssaint.utils smoothScrollEnabled -bool true
defaults write com.vorssaint.utils scrollInverterHorizontalEnabled -bool false
defaults write com.vorssaint.utils windowEdgeSnapEnabled -bool true
defaults write com.vorssaint.utils shelfEnabled -bool false

# Screenshots
defaults write com.vorssaint.utils screenshotShortcutEnabled -bool true
defaults write com.vorssaint.utils screenshotShortcut -string "shift+command:21"

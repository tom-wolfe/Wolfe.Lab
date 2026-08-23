#!/bin/bash

set -euo pipefail

# Dock
defaults write com.apple.dock tilesize -int 61
defaults write com.apple.dock orientation -string left
defaults write com.apple.dock show-recents -bool false

# Finder
defaults write com.apple.finder ShowPathbar -bool true
defaults write com.apple.finder ShowStatusBar -bool true
defaults write com.apple.finder FXPreferredViewStyle -string Nlsv
defaults write NSGlobalDomain AppleShowAllExtensions -bool true

# Appearance (new apps only; a logout applies it everywhere)
defaults write NSGlobalDomain AppleInterfaceStyle -string Dark

# Screenshots
mkdir -p "$HOME/Documents/Screenshots"
defaults write com.apple.screencapture location -string "$HOME/Documents/Screenshots"

killall Dock Finder 2>/dev/null || true

#!/bin/bash

# REQUIREMENTS
#   You must have Docker installed and working.
#   You must have the Google Cloud SDK installed.
#   You must have run the command `gcloud auth configure-docker`

##### This sets up color codes if the terminal supports colors. #####
# Check if stdout is a terminal...
if test -t 1; then

    # see if it supports colors...
    ncolors=$(tput colors)

    if test -n "$ncolors" && test $ncolors -ge 8; then
        bold="$(tput bold)"
        underline="$(tput smul)"
        standout="$(tput smso)"
        normal="$(tput sgr0)"
        black="$(tput setaf 0)"
        red="$(tput setaf 1)"
        green="$(tput setaf 2)"
        yellow="$(tput setaf 3)"
        blue="$(tput setaf 4)"
        magenta="$(tput setaf 5)"
        cyan="$(tput setaf 6)"
        white="$(tput setaf 7)"
    fi
fi

# Builds a Docker image and tags it tagrides:latest
if docker-compose build ; then
  echo "${green}Build successful${normal}"
else
  echo "${red}Build failed. Stopping.${normal}"
  exit 1
fi

# Using Docker with Google Cloud: https://cloud.google.com/container-registry/docs/quickstart

# Tag the image locally; required for the next command because
# Docker uses tags for setting the upload address.
docker tag tagrides gcr.io/tagraidesteam/tagrides:latest

# Upload the image to Google's servers.
docker push gcr.io/tagraidesteam/tagrides:latest

# Now you can use `gcloud container images list-tags gcr.io/tagraidesteam/tagrides`
# to check the newly pushed image.
# See https://cloud.google.com/container-registry/docs/pushing-and-pulling

# See this for info on managing images: https://cloud.google.com/container-registry/docs/managing

# It's easiest to create an instance through the web interface.
# If you choose to use the command line, remember to specify --machine-type=f1-micro

oldAddress=$( bash get-server-address.sh )

# Update running instance with the new image
gcloud compute instances update-container develop-server \
    --container-image gcr.io/tagraidesteam/tagrides:latest

# We use a static IP, but if we didn't, the following code would
# print out the updated IP address.
echo "Server address: ${bold}"$( bash get-server-address.sh )"${normal}"
echo "Previous address: ${bold}${oldAddress}${normal}"

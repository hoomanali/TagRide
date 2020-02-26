#!/bin/bash
gcloud compute instances describe develop-server \
  --format='get(networkInterfaces[0].accessConfigs[0].natIP)'

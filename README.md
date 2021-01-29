
# Log location config
```
export OXIDE_ROOT_DIR="/root/.steam/steamapps/common/rust_dedicated/oxide"
ln -s /dev/shm ${OXIDE_ROOT_DIR}/logs/RustEventLogger
```

# Filebeat Configuration
```yaml
---

setup.dashboards.enabled: true
setup.ilm.enabled: false

setup.kibana.host: "https://${KIBANA_HOST}:5601"
setup.kibana.username: ${KIBANA_USERNAME}
setup.kibana.password: ${KIBANA_PASSWORD}
setup.kibana.ssl.enabled: true
setup.kibana.ssl.verification_mode: none

filebeat.modules:
- module: system

filebeat.inputs:
- type: log
  enabled: true
  index: "rust-events-%{+xxx-ww}"
  paths:
  - /root/.steam/steamapps/common/rust_dedicated/oxide/logs/RustEventLogger/*
  json.keys_under_root: true
  
- type: log
  enabled: true
  index: "rust-logs-%{+xxx-ww}"
  paths:
  - /var/log/rust.log
  - /root/.steam/steamapps/common/rust_dedicated/oxide/logs/*

processors:
- add_fields:
    target: ''
    fields:
      instance_id: big-frog
      
- decode_json_fields:
    fields: ["message"]

output.elasticsearch:
  hosts: ["https://${ELASTICSEACH_HOST}:9200"]
  username: ${ELASTICSEACH_USERNAME}
  password: ${ELASTICSEACH_PASSWORD}
  ssl.verification_mode: none
  pipeline: geoip
```
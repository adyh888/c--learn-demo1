#!/bin/bash
API="http://localhost:5189/api/device"

echo "1. 获取所有设备"
curl -s $API | json_pp

echo "\n2. 获取设备详情"
curl -s $API/1 | json_pp

echo "\n3. 创建新设备"
curl -s -X POST $API \
  -H "Content-Type: application/json" \
  -d '{
    "name": "压力传感器-01",
    "description": "管道压力监控",
    "type": 1,
    "ipAddress": "192.168.1.103",
    "port": 502
  }' | json_pp

echo "\n4. 筛选传感器类型设备"
curl -s "$API?type=1" | json_pp

echo "\n5. 添加设备数据"
curl -s -X POST $API/1/data \
  -H "Content-Type: application/json" \
  -d '{
    "dataType": "温度",
    "value": 27.3,
    "unit": "°C"
  }' | json_pp

echo "\n6. 获取设备数据"
curl -s $API/1/data | json_pp
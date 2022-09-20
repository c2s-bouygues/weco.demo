import time
import random
import uuid
import string
import json
from datetime import datetime
from locust import HttpUser, task, between

class WeCoUsers(HttpUser):
    # Temps d'attente moyen entre chaque task de user
    wait_time = between(0.5, 2)

    def __init__(self, http):
        super().__init__(http)
        # Générateur de nombre aléatoire
        self.rnd = random.Random()

    @task
    def setSpeedMeasure(self):
        #https://egu-weco-demo-func-planes-ingester.azurewebsites.net/api/swagger/ui?#/planes/SetSpeedMeasure
        #https://egu-weco-demo-func-planes-ingester.azurewebsites.net/api/SetSpeedMeasure?code=Fuqn84rNQOSUy6falT-ZYFDQW_odxMLOUZtp8LYOevziAzFuGL1i7g==

        coords = (self.rnd.random()*2.0, self.rnd.random()*2.0)
        str_1 = ''.join(random.choice(string.ascii_lowercase) for i in range(8))
        str_2 = ''.join((random.choice('0123456789') for i in range(5)))
        temps = [round(rnd.uniform(0, 25), 2), round(rnd.uniform(0, 25), 2), round(rnd.uniform(0, 25), 2)]
        temps.sort()

        new_speed_measure = {
            "deviceId": str(uuid.uuid4()),
            "deviceName": str_1 + '-' + str_2,
            "category": "category_speed_" + str(self.rnd.randint(0, 20)),
            "coordinates": {
                "longitude": coords[0],
                "latitude": coords[1]
            },
            "velocity": [
                {
                    "id": str(uuid.uuid4()),
                    "type": "speed",
                    "value": self.rnd.randint(400, 500),
                    "unit": "knots",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                },
                {
                    "id": str(uuid.uuid4()),
                    "type": "alt",
                    "value": self.rnd.randint(9000, 12000),
                    "unit": "m",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                },
                {
                    "id": str(uuid.uuid4()),
                    "type": "dir",
                    "value": self.rnd.randint(0, 359),
                    "unit": "deg",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                }
            ],
            "temperatures": [
                {
                    "id": str(uuid.uuid4()),
                    "type": "temp",
                    "value": temps[1],
                    "unit": "degC",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                },
                {
                    "id": str(uuid.uuid4()),
                    "type": "temp_min",
                    "value": temps[0],
                    "unit": "degC",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                },
                {
                    "id": str(uuid.uuid4()),
                    "type": "temp_max",
                    "value": temps[2],
                    "unit": "degC",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                }
            ]
        }

        payload = json.dumps(new_speed_measure)
        print(payload)

        self.client.post('https://egu-weco-demo-func-planes-ingester.azurewebsites.net/api/SetSpeedMeasure?code=Fuqn84rNQOSUy6falT-ZYFDQW_odxMLOUZtp8LYOevziAzFuGL1i7g==', data=payload) 

        time.sleep(self.rnd.randint(1, 5))

    @task 
    def setGasMeasure(self):
        #https://egu-weco-demo-func-gas-ingester.azurewebsites.net/api/swagger/ui#/gas/SetGasMeasure
        #https://egu-weco-demo-func-gas-ingester.azurewebsites.net/api/SetGasMeasure?code=B1qYqAfbjN9j9mJXxkmOq0QUbxkzzdtOeoS8BDWyDDAEAzFumGxE6g==
        coords = (self.rnd.random()*2.0, self.rnd.random()*2.0)
        str_1 = ''.join(random.choice(string.ascii_lowercase) for i in range(8))
        str_2 = ''.join((random.choice('0123456789') for i in range(5)))
        azote_level = round(self.rnd.uniform(10, 30), 2)

        new_gas_measure = {
            "deviceId": str(uuid.uuid4()),
            "deviceName": str_1 + '-' + str_2,
            "category": "category_gas_" + str(self.rnd.randint(0, 20)),
            "coordinates": {
                "longitude": coords[0],
                "latitude": coords[1]
            },
            "measures": [
                {
                "id": str(uuid.uuid4()),
                "type": "co2",
                "value": round(self.rnd.uniform(350, 450), 2),
                "unit": "ppm",
                "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                },
                {
                "id": str(uuid.uuid4()),
                "type": "azote",
                "value": azote_level,
                "unit": "%",
                "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                },
                {
                "id": str(uuid.uuid4()),
                "type": "oxygen",
                "value": (100 - azote_level),
                "unit": "%",
                "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                }
            ],
            "wind": {
                "speed": self.rnd.randint(0, 300),
                "deg": self.rnd.randint(0, 80)
            }
        }

        payload = json.dumps(new_gas_measure)
        print(payload)

        self.client.post('https://egu-weco-demo-func-gas-ingester.azurewebsites.net/api/SetGasMeasure?code=B1qYqAfbjN9j9mJXxkmOq0QUbxkzzdtOeoS8BDWyDDAEAzFumGxE6g==', data=payload)

        time.sleep(self.rnd.randint(1, 5))
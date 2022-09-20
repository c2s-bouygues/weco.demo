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

        velocity_types = ["speed", "alt", "dir"]
        temp_types = ["temp", "temp_min", "temp_max"]
        coords = (self.rnd.random()*2.0, self.rnd.random()*2.0)
        str_1 = ''.join(random.choice(string.ascii_lowercase) for i in range(8))
        str_2 = ''.join((random.choice('0123456789') for i in range(5)))

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
                    "id": self.rnd.randint(0, 999999),
                    "type": self.rnd.choice(velocity_types),
                    "value": self.rnd.randint(0, 300),
                    "unit": "string",
                    "timestamp": datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
                }
            ],
            "temperatures": [
                {
                    "id": self.rnd.randint(0, 999999),
                    "type": self.rnd.choice(temp_types),
                    "value": self.rnd.randint(0, 50),
                    "unit": "c",
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
        gas_types = ["co2", "azote", "oxygen"]
        coords = (self.rnd.random()*2.0, self.rnd.random()*2.0)
        str_1 = ''.join(random.choice(string.ascii_lowercase) for i in range(8))
        str_2 = ''.join((random.choice('0123456789') for i in range(5)))

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
                "type": self.rnd.choice(gas_types),
                "value": self.rnd.randint(0, 500),
                "unit": "ppm",
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
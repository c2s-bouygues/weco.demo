import time
import random
import uuid
from faker import Faker
from datetime import datetime
from locust import HttpUser, task, between

fake = Faker()

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

        velocityTypes = ["speed", "alt", "dir"]
        tempTypes = ["temp", "temp_min", "temp_max"]

        newSpeedMeasure = {
            "deviceId": uuid.uuid4(),
            "deviceName": fake.bothify(text='????-########'),
            "category": fake.color_name(),
            "coordinates": {
                "longitude": fake.longitude(),
                "latitude": fake.latitude()
            },
            "velocity": [
                {
                    "id": uuid.uuid4(),
                    "type": random.choice(velocityTypes),
                    "value": fake.random_int(min=0, max=300),
                    "unit": "string",
                    "timestamp": datetime.now()
                }
            ],
            "temperatures": [
                {
                    "id": uuid.uuid4(),
                    "type": random.choice(tempTypes),
                    "value": fake.random_int(min=0, max=50),
                    "unit": "c",
                    "timestamp": datetime.now()
                }
            ]
        }

        response = self.client.post('https://egu-weco-demo-func-planes-ingester.azurewebsites.net/api/SetSpeedMeasure?code=Fuqn84rNQOSUy6falT-ZYFDQW_odxMLOUZtp8LYOevziAzFuGL1i7g==', json=newSpeedMeasure)
        # TODO: Check response code

        time.sleep(self.rnd.randint(1, 5))

    @task 
    def setGasMeasure(self):
        #https://egu-weco-demo-func-gas-ingester.azurewebsites.net/api/swagger/ui#/gas/SetGasMeasure
        #https://egu-weco-demo-func-gas-ingester.azurewebsites.net/api/SetGasMeasure?code=B1qYqAfbjN9j9mJXxkmOq0QUbxkzzdtOeoS8BDWyDDAEAzFumGxE6g==
        gasTypes = ["co2", "azote", "oxygen"]

        newGasMeasure = {
            "deviceId": uuid.uuid4(),
            "deviceName": fake.bothify(text='????-########'),
            "category": fake.color_name(),
            "coordinates": {
                "longitude": fake.longitude(),
                "latitude": fake.latitude()
            },
            "measures": [
                {
                "id": uuid.uuid4(),
                "type": random.choice(gasTypes),
                "value": fake.random_int(min=0, max=500),
                "unit": "ppm",
                "timestamp": datetime.now()
                }
            ],
            "wind": {
                "speed": fake.random_int(min=0, max=300),
                "deg": fake.random_int(min=0, max=80)
            }
        }

        response = self.client.post('https://egu-weco-demo-func-gas-ingester.azurewebsites.net/api/SetGasMeasure?code=B1qYqAfbjN9j9mJXxkmOq0QUbxkzzdtOeoS8BDWyDDAEAzFumGxE6g==', json=newGasMeasure)
        # TODO: Check response code

        time.sleep(self.rnd.randint(1, 5))
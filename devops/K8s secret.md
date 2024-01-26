# Running in Kubernetes with interpolated secrets
Microservices built on top of Liquid Application Framework, when running in Kubernetes pods automatically look for a secret called server-secret having sensitive key and connection information to access underlying services such as database, storage, message bus, autentication, etc.

# Define keys in json settings as <YOUR_KEY_SOMETHING>
By defining a value of a key in microservice's json settings , the framework look in the server secret file to interpolate keys and values.

Therefore, no sensitive runtime information is needed to be stored along with the source code of microservices.
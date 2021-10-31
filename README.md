# ConQuiz
Trivia PvP game set in Antarctica. Core concept of the game is to capture territories from other players during the game. The end score is based on the amount of territories and points you control.

# Architecture
This repository contains all the backend logic and kubernetes configuration files for the ConQuiz project. The front-end React Native repository is located <a href="https://github.com/BoostedPenguin/ConQuiz-Frontend">here</a>.
<img src="https://i.imgur.com/3U47lfP.png" />

# Backend stack:
* NET Core 3.1
* SQL Server (Entity ORM)
* GRPC
* JWT Authentication
* RabbitMQ
* Docker
* Kubernetes
* SignalR

## Communication between Microservices
Embracing eventual consistency pattern, we use the RabbitMQ message bus to send messages between the microservices and we "pull" for any missing data whenever a microservice starts.

<img src="https://i.imgur.com/kO8WVuO.png" />


# Run local production cluster
If you want to run ConQuiz locally with Kubernetes you need the following pre-requisites:

* Docker Desktop installed
* Kubernetes enabled in docker desktop

*Create a namespace for the cluster if you don't want it to be stored in the default one*

## Create MSSQL Secret
```
kubectl create secret generic mssql --from-literal=SA_PASSWORD="yourpassword"
```

## Apply all configuration files
In the K8S Folder run this command:

```
kubectl apply -f .
```
This will generate all services, deployments, a persistant volume claim and an nginx ingress controller.

You can then access the backend production web server on https://localhost/api/account
If you issued a self-signed SSL certificate you'd be prompted to allow access to this URL

## Microservices endpoints
There are currently 2 microservices running on a single node:
* AccountService - host/api/account
* GameService - host/api/game

## Use https 
The kubernetes cluster doesn't communicate with HTTPS between the pods, however if you want to expose it to the public you have to make sure that the public IP has valid HTTPS.
We use Cert Manager to manage our SSL Certificates

```
kubectl apply --validate=false -f cert-manager.yaml
```

If you want a self-signed certificate you need to apply these 2 files in the selfsigned directory

```
kubectl apply -f issuer.yaml
kubectl apply -f certificate.yaml
```

## Open to public
If you want to expose the K8S cluster to the public you need to either connect a DNS through google or use tunneling when port forwarding isn't available on the machine (ISP blocking it)

To open it through tunnel you can use NGrok
```
ngrok http -region=eu 443
```

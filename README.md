<h1 align="center"> ConQuiz </h1>
Trivia PvP game based on Triviador, set in Antarctica. ConQuiz is a round-based PvP trivia game revolved around capturing the territories of the continent of Antarctica. The end score is based on the amount of territories and points you control.
<br />
<br />
This repository contains all the backend logic and kubernetes configuration files for the ConQuiz project. The front-end React Native repository is located <a href="https://github.com/BoostedPenguin/ConQuiz-Frontend">here</a>.

# Table of contents
1. [Game rules](#gamerules)
2. [Architecture](#architecture)
3. [Backend stack](#backendstack)
3. [Run local production cluster](#runninglocalcluster)


# Game rules <a name="gamerules" />

<img src="https://i.imgur.com/SiB9DFV.png" />

# Architecture <a name="architecture" />
The backend consists of 3 microservices which utilize RabbitMQ and GRPC to communicate with each other and send messages via SignalR to the frontend React Native application.

<img src="https://i.imgur.com/kaaqNMW.png" />


# Backend stack: <a name="backendstack" />
* NET Core 3.1
* SQL Server (Entity ORM)
* SignalR
* GRPC
* JWT Authentication
* RabbitMQ
* Docker
* Kubernetes

## Communication between Microservices <a name="microservicescommunication" />
Embracing eventual consistency pattern, we use the RabbitMQ message bus to send messages between the microservices and we "pull" for any missing data whenever a microservice starts.

<img src="https://i.imgur.com/kO8WVuO.png" />


# Run local production cluster <a name="runninglocalcluster" />
If you want to run ConQuiz locally with Kubernetes you need the following pre-requisites:

* Docker Desktop installed
* Kubernetes enabled in docker desktop

*Create a namespace for the cluster if you don't want it to be stored in the default one*

## Create MSSQL Secret <a name="mssqlsecret" />
```
kubectl create secret generic mssql --from-literal=SA_PASSWORD="yourpassword"
```

## Apply all configuration files <a name="configfiles" />
In the K8S Folder run this command:

```
kubectl apply -f .
```
This will generate all services, deployments, keel.sh, a persistant volume claim and an nginx ingress controller.

*There is a possibility that some deployments will not be registered, so make sure to verify if all services in the K8S directory are running*

You can then access the backend production web server on https://localhost/api/account
If you issued a self-signed SSL certificate you'd be prompted to allow access to this URL

## Add ConfigMaps for deployment environmental variables <a name="envvariables" />
Each microservice has multiple environmental variables which are usually stored in an appsettings.json, however for docker we inject them through a ConfigMap. There are 3 example configmaps in the K8S directory. Add your own secrets, endpoints etc. and apply them.

## Microservices endpoints <a name="microservicesendpoints" />
There are currently 3 microservices running on a single node:
* AccountService - {host}/api/account
* GameService - {host}/api/game
* QuestionService - {host}/api/question

Backend is also using Keel.sh to poll dockerhub for new container images and redeploys a service if it finds a new digest.

## Use https <a name="usehttps" />
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

## Open to public <a name="opentopublic" />
If you want to expose the K8S cluster to the public you need to either connect a DNS through google or use tunneling when port forwarding isn't available on the machine (ISP blocking it)

To open it through tunnel you can use NGrok
```
ngrok http -region=eu 443
```

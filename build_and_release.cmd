@echo off
docker build -t nortagesbot . && heroku container:push -a nortagesbot web && heroku container:release -a nortagesbot web
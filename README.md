# AnnovarWebui

```bash
git clone https://github.com/fo40225/AnnovarWebui.git
cd AnnovarWebui
sudo docker build -t annovarwebui:0.0.1 -f AnnovarWebui/Dockerfile .
sudo docker run -d -p 80:8080 -e ANNOVAR_PATH=$ANNOVAR_PATH -v $ANNOVAR_PATH:$ANNOVAR_PATH --restart unless-stopped annovarwebui:0.0.1
```

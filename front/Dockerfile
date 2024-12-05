# Utiliser l'image officielle de Nginx
FROM nginx:latest

# Copier les fichiers HTML et statiques dans le répertoire par défaut de Nginx
COPY . /usr/share/nginx/html

# Expose le port 80 pour servir les fichiers
EXPOSE 80

# Lancer Nginx en tant que commande par défaut
CMD ["nginx", "-g", "daemon off;"]

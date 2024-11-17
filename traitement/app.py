from flask import Flask, request, jsonify
import requests
from flask_cors import CORS  # Importer CORS

app = Flask(__name__)

# Activer CORS pour toutes les routes
CORS(app)

last_result = None

@app.route('/compute', methods=['POST'])
def compute():
    global last_result
    if request.method == 'POST':
        try:
            num1 = request.form.get('num1', type=int)  # Récupérer les données envoyées
            num2 = request.form.get('num2', type=int)
            result = num1 + num2  # Exemple de calcul
            last_result = result
            
            # Envoi du résultat au service C#
            csharp_endpoint = "http://127.0.0.1:5225/receive_result"  # Changez l'URL en celle du service C#
            payload = {"result": result}
            
            try:
                response = requests.post(csharp_endpoint, json=payload)
                response.raise_for_status()  # Vérifie si la requête a réussi
                return jsonify({"result": result, "csharp_status": "Success"})
            except requests.exceptions.RequestException as e:
                return jsonify({"result": result, "csharp_status": f"Failed to send: {str(e)}"}), 500

        except Exception as e:
            return jsonify({"error": f"Problème lors du chargement des données JSON : {str(e)}"}), 400

# Fonction pour récupérer toutes les valeurs stockées dans le service C#
@app.route('/get-results-from-csharp', methods=['GET'])
def get_results_from_csharp():
    url = "http://127.0.0.1:5225/get_results"  # URL du service C#

    try:
        # Effectuer une requête GET pour récupérer les résultats
        response = requests.get(url)
        response.raise_for_status()  # Vérifie si la requête a réussi

        # Récupérer la réponse JSON
        results = response.json()  # Parse la réponse JSON
        return jsonify(results)  # Renvoyer les résultats en format JSON
        
    except requests.exceptions.RequestException as e:
        # En cas d'erreur, renvoyer un message d'erreur
        return jsonify({"error": f"Erreur lors de la récupération des résultats : {str(e)}"}), 500


if __name__ == "__main__":
    app.run(debug=True)

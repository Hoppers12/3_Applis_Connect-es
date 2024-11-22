from flask import Flask, request, jsonify
import requests
from flask_cors import CORS  # Importer CORS

app = Flask(__name__)

# Activer CORS pour toutes les routes
CORS(app)

last_result = None


@app.route('/compute', methods=['POST'])
#Fonction qui fait le calcul et l'envoie au C# pour stockage
def compute():
    global last_result
    if request.method == 'POST':
        try:
            num1 = request.form.get('num1', type=int)  # Récupérer les données envoyées
            num2 = request.form.get('num2', type=int)
            result = num1 + num2  # Exemple de calcul
            last_result = result

            isPair = testPair(result)
            isPremier = testPremier(result)
            isParfait = testParfait(result)

            tab_result = [result,isPair,isPremier,isParfait]
            
            print("tab result" ,tab_result)
            # Envoi du résultat au service C#
            csharp_endpoint = "http://127.0.0.1:5225/receive_result"  # Changez l'URL en celle du service C#
            payload = {"tab_result": tab_result}
            
            try:
                response = requests.post(csharp_endpoint, json=payload)
                response.raise_for_status()  # Vérifie si la requête a réussi

                #On envoie un tableau contenant : [resultat,ispair,ispremier,isparfait]
                return jsonify({"result": tab_result, "csharp_status": "Success"})
            
            except requests.exceptions.RequestException as e:

                return jsonify({"result": result, "csharp_status": f"Failed to send: {str(e)}"}), 500

        except Exception as e:
            return jsonify({"error": f"Problème lors du chargement des données JSON : {str(e)}"}), 400



def testParfait(nombre):
    """
    Vérifie si un nombre est parfait.
    Un nombre parfait est égal à la somme de ses diviseurs propres (sauf lui-même).
    retourne true si parfait false sinon
    """
    if nombre <= 0:
        return False
    somme_diviseurs = sum(i for i in range(1, nombre // 2 + 1) if nombre % i == 0)
    return somme_diviseurs == nombre


def testPremier(nombre):
    """
    Vérifie si un nombre est premier.
    """
    if nombre <= 1:
        return False
    for i in range(2, int(nombre ** 0.5) + 1):
        if nombre % i == 0:
            return False
    return True


def testPair(valeur):
    if (valeur%2 == 0) :
        return True
    else : 
        return False

# Fonction pour récupérer toutes les valeurs stockées dans le service C#
#Cette fonction sera appelé par le front pr l'affichage par la suite
@app.route('/get-results-from-csharp', methods=['GET'])
def get_results_from_csharp():
    url = "http://127.0.0.1:5225/get_results"  # URL du service C#

    try:
        response = requests.get(url)
        response.raise_for_status() 

        # Récupérer la réponse JSON
        results = response.json()  
        return jsonify(results)   
        
    except requests.exceptions.RequestException as e:
 
        return jsonify({"error": f"Erreur lors de la récupération des résultats : {str(e)}"}), 500


if __name__ == "__main__":
    app.run(debug=True)

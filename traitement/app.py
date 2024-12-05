from flask import Flask, request, jsonify
import requests
from flask_cors import CORS
app = Flask(__name__)

CORS(app)
#Fonction qui s'occupe des calculs et d'aiguiller vers le bonnes fonctions
@app.route('/compute', methods=['POST'])
def compute():
    if request.method == 'POST':
        resultats2 = get_results_from_csharp()
        resultats2_data = resultats2.get_json() if hasattr(resultats2, "get_json") else {}

        num1 = request.form.get('num1', type=int)
        num2 = request.form.get('num2', type=int)
        value_compute = 0

        match_found, value_compute = matchFound(resultats2_data, num1, num2)

        if match_found:
            return jsonify({"result": str(value_compute) + " (calcul déjà stocké en base)"})
        else:
            tab_result, syracuse = calculProjet(num1, num2)
            send_numbers_to_csharp(syracuse)

            csharp_endpoint = "http://127.0.0.1:5225/receive_result"
            payload = {"tab_result": tab_result}

            try:
                response = requests.post(csharp_endpoint, json=payload)
                response.raise_for_status()

                return jsonify({"result": tab_result[0], "csharp_status": "Success"})
            except requests.exceptions.RequestException as e:
                return jsonify({"result": tab_result, "csharp_status": f"Failed to send: {str(e)}"}), 500
            except Exception as e:
                return jsonify({"error": f"Problème lors du chargement des données JSON : {str(e)}"}), 400

#Fonction qui envoie la suite de syracuse à l'api C#
def send_numbers_to_csharp(numbers):
    url = "http://127.0.0.1:5225/upload_numbers"
    payload = numbers

    try:
        response = requests.post(url, json=payload)
        response.raise_for_status()
    except requests.exceptions.RequestException as e:
        print(f"[ERREUR] Erreur lors de l'envoi de la requête : {str(e)}")

#Fonction qui vérifie si un calcul n'a pas déja été fait dans datas
def matchFound(datas, num1, num2):
    match_found = False
    value_compute = 0
    for item in datas:
        if (item["val1"] == num1 and item["val2"] == num2) or (item["val1"] == num2 and item["val2"] == num1):
            match_found = True
            value_compute = item['ComputedResult']
    return match_found, value_compute

#Fonction qui retourne les calculs appliqués sur 2 valeurs sous la forme d'un tableau + syracuse
def calculProjet(num1, num2):
    result = num1 + num2
    isPair = testPair(result)
    isPremier = testPremier(result)
    isParfait = testParfait(result)
    syracuse_seq = syracuse(result)
    tab_result = [result, num1, num2, isPair, isPremier, isParfait]

    return tab_result, syracuse_seq

def syracuse(x):
    sequence = [x]
    while x != 1:
        if x % 2 == 0:
            x = x // 2
        else:
            x = 3 * x + 1
        sequence.append(x)

    return sequence

def testParfait(nombre):
    if nombre <= 0:
        return False
    somme_diviseurs = sum(i for i in range(1, nombre // 2 + 1) if nombre % i == 0)
    return somme_diviseurs == nombre

def testPremier(nombre):
    if nombre <= 1:
        return False
    for i in range(2, int(nombre ** 0.5) + 1):
        if nombre % i == 0:
            return False
    return True

def testPair(valeur):
    return valeur % 2 == 0

#Fonction qui récupère les résultats venant de l'api c#
@app.route('/get-results-from-csharp', methods=['GET'])
def get_results_from_csharp():
    url = "http://127.0.0.1:5225/get_results"

    try:
        response = requests.get(url)
        response.raise_for_status()

        results = response.json()
        return jsonify(results)
    except requests.exceptions.RequestException as e:
        return jsonify({"error": f"Erreur lors de la récupération des résultats : {str(e)}"}), 500

if __name__ == "__main__":
    app.run(debug=True)

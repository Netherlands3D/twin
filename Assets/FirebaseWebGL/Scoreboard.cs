using System.Collections.Generic;
using UnityEngine;
using FirebaseWebGL.Database;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace Netherlands3D.Twin
{
   

    public class ScoreboardManager : MonoBehaviour
    {
        public int scoreListCount = 5;
        [SerializeField] private Timer timer;
        public TMP_InputField nameInputField;
        public TMP_Text scoreField;
        public Button submitButton;
        public TMP_Text scoreboardText;

        private string userId;
        private const string ScoreboardPath = "test/scoreboard"; // Firebase database path for scoreboard

        void OnEnable()
        {
            scoreField.text = ((int)timer.FinishedTime).ToString();
            nameInputField.gameObject.SetActive(true);
            submitButton.gameObject.SetActive(true);
        }

        void Start()
        {
            nameInputField.characterLimit = 10;
        // Attach button click listener
            submitButton.onClick.AddListener(() => SubmitScore());

            // Listen for updates to the scoreboard
            ListenForScoreboardUpdates();
        }

        public string SanitizeInput(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9\s]", ""); // Allows only letters, numbers, and spaces
        }        

        void SubmitScore()
        {
            string userName = nameInputField.text;
            string userScore = scoreField.text;

            nameInputField.gameObject.SetActive(false);
            submitButton.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userScore))
            {
                Debug.LogError("Name or score cannot be empty!");
                return;
            }

            SanitizeInput(userName);

            // Create a score entry
            var scoreData = new Dictionary<string, object>
            {
                { "name", userName },
                { "score", int.Parse(userScore) }
            };

            // Push the score to Firebase
            string json = JsonConvert.SerializeObject(scoreData);
            //Debug.Log("JSON DATA BEING PUSHED: " + json);
            FirebaseDatabase.PostJSON(ScoreboardPath + "/" + FirebaseInit.userId, json, gameObject.name, nameof(OnScoreSubmitSuccess), nameof(OnScoreSubmitFailure));
        }

        public void OnScoreSubmitSuccess(string output)
        {
            Debug.Log("Score submitted successfully: " + output);
        }

        public void OnScoreSubmitFailure(string output)
        {
            Debug.LogError("Failed to submit score: " + output);
        }

        void ListenForScoreboardUpdates()
        {
            FirebaseDatabase.ListenForValueChanged(ScoreboardPath, gameObject.name, nameof(OnScoreboardUpdated), nameof(OnScoreboardUpdateFailed));
        }

        public void OnScoreboardUpdated(string json)
        {
            //Debug.Log("Scoreboard updated: " + json);

            // Deserialize JSON and update the UI
            DisplayScores(json);
        }

        public void OnScoreboardUpdateFailed(string output)
        {
            Debug.LogError("Failed to listen for scoreboard updates: " + output);
        }

        void DisplayScores(string json)
        {
            var scores = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Dictionary<string, Score> scoreList = new Dictionary<string, Score>();           
            scoreboardText.text = "";
            foreach (KeyValuePair<string, object> kvp in scores)
            {
                //Debug.Log("checking entry:" + kvp.Key);
                //bool isDic = kvp.Value is Dictionary<string, object>;
                //Debug.Log(kvp.Value + " isdiciton: " + isDic);

                if (kvp.Value is JObject jObject)
                {
                    Score score = new Score();
                    score.name = "";
                    score.score = -1;
                    Dictionary<string, object> entry = jObject.ToObject<Dictionary<string, object>>();
                    foreach (KeyValuePair<string, object> entryKvp in entry)
                    {
                        if (entryKvp.Key == "name")
                            score.name = entryKvp.Value.ToString();
                        if (entryKvp.Key == "score")
                            score.score = int.Parse(entryKvp.Value.ToString());
                    }
                    scoreList.TryAdd(kvp.Key, score);
                }
            }
            var sortedDictionary = scoreList.OrderBy(kvp => kvp.Value.score).Take(scoreListCount).ToList();
            int rank = 1;
            foreach (var kvp in sortedDictionary)
            {                
                scoreboardText.text += $"Rank: {rank}, Name: {kvp.Value.name}, Score: {kvp.Value.score.ToString()} \n";
                rank++;
            }
        }

        [System.Serializable]
        public class Score
        {
            public string name;
            public int score;
        }

        private void OnDestroy()
        {
            FirebaseDatabase.StopListeningForValueChanged(ScoreboardPath, gameObject.name, nameof(OnSuccess), nameof(OnError));
        }

        private void OnSuccess(string msg)
        {
            Debug.Log(msg);
        }

        private void OnError(string msg)
        {
            Debug.Log(msg);
        }
    }
}

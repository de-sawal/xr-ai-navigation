using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TrialData
{
    public string participantId;
    public int trialNumber;
    public NavigationStyle condition;
    public float completionTime;
    public bool isCorrect;
    public System.DateTime timestamp;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public float totalDistance;
    public int errors;
}

[System.Serializable]
public class SessionData
{
    public string participantId;
    public System.DateTime startTime;
    public System.DateTime endTime;
    public List<TrialData> trials = new List<TrialData>();
}

public class DataLogger : MonoBehaviour
{
    private SessionData currentSession;
    private string participantId;
    private string dataPath;

    private void Awake()
    {
        dataPath = System.IO.Path.Combine(Application.persistentDataPath, "ExperimentData");
        System.IO.Directory.CreateDirectory(dataPath);
    }

    public void StartNewSession()
    {
        currentSession = new SessionData
        {
            participantId = participantId,
            startTime = System.DateTime.Now
        };
    }

    public void SetParticipantId(string id)
    {
        participantId = id;
    }

    public string GetParticipantId()
    {
        return participantId;
    }

    public void LogTrial(TrialData trial)
    {
        if (currentSession == null)
        {
            Debug.LogError("Attempting to log trial without active session");
            return;
        }

        currentSession.trials.Add(trial);
        SaveSessionData();
    }

    public void EndSession()
    {
        if (currentSession == null) return;

        currentSession.endTime = System.DateTime.Now;
        SaveSessionData();
        ExportData();
    }

    private void SaveSessionData()
    {
        string filename = $"session_{participantId}_{currentSession.startTime:yyyyMMdd_HHmmss}.json";
        string filepath = System.IO.Path.Combine(dataPath, filename);
        string json = JsonUtility.ToJson(currentSession, true);
        System.IO.File.WriteAllText(filepath, json);
    }

    private string ExportData()
    {
        if (currentSession == null) return string.Empty;

        string csvPath = System.IO.Path.Combine(dataPath,
            $"data_export_{participantId}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        using (var writer = new System.IO.StreamWriter(csvPath))
        {
            writer.WriteLine("ParticipantID,TrialNumber,Condition,CompletionTime,IsCorrect,Timestamp,StartPosition,EndPosition,TotalDistance");

            foreach (var trial in currentSession.trials)
            {
                writer.WriteLine(
                    $"{trial.participantId},{trial.trialNumber},{trial.condition}," +
                    $"{trial.completionTime},{trial.isCorrect},{trial.timestamp}," +
                    $"{trial.startPosition},{trial.endPosition},{trial.totalDistance}"
                );
            }
        }

        return csvPath;
    }
}


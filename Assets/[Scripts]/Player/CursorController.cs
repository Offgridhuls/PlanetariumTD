using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour
{

    private bool m_CursorActive;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        m_CursorActive = false;
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            ToggleCursorMode();
        }
    }
    public void ToggleCursorMode()
    {
        if (!m_CursorActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            m_CursorActive = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            m_CursorActive = false;
        }
    }

    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        m_CursorActive = true;
    }
    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        m_CursorActive = false;
    }
    public bool IsCursorActive()
    {
        return m_CursorActive;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class HelperUtilities
{
    public static bool ValidateCheckEmptyString(Object thisObject, string fieldname, string stringtoCheck)
    {
        if (stringtoCheck == "")
        {
            Debug.Log(fieldname + "is Empty and must contain a value in Object" + thisObject.name.ToString());
            return true;
        }
        return false;
    }

    public static bool ValidateCheckEnumerableValues(Object thisObject, string fieldname,IEnumerable enumerableObjectToCheck)
    {
        bool error = false;
        int count = 0;

        if(enumerableObjectToCheck == null)
        {
            Debug.Log(fieldname+" -is null in object- "+thisObject.name.ToString());
            return true;
        }

        foreach (var item in enumerableObjectToCheck)
        {
            if (item == null)
            {
                Debug.Log(fieldname + "- has null values in object -" + thisObject.name.ToString());
                error = true;
            }
            else
            {
                count++;
            }
        }
            if(count == 0)
            {
            Debug.Log(fieldname + " has no values in object " + thisObject.name.ToString());
            error = true;
            }
           
        
        return error;
    }
}
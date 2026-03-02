// RoomPicker.cs
using System.Collections.Generic;
using UnityEngine;

public class RoomPicker : Room
{
    public ValidateRoom[] roomChoices;

    public override Room createRoom(ExitConstraint requiredExits)
    {
        List<ValidateRoom> validRooms = new List<ValidateRoom>();

        foreach (ValidateRoom room in roomChoices)
        {
            if (room.MeetsConstraints(requiredExits))
            {
                validRooms.Add(room);
            }
        }

        ValidateRoom roomPrefab = GlobalFuncs.randElem(validRooms);
        return roomPrefab.GetComponent<Room>().createRoom(requiredExits);
    }
}
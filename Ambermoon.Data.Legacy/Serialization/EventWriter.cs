﻿using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal class EventWriter
    {
        public static void WriteEvents(IDataWriter dataWriter,
            List<Event> events, List<Event> eventList)
        {
            dataWriter.Write((ushort)eventList.Count);
            
            foreach (var @event in eventList)
            {
                dataWriter.Write((ushort)events.IndexOf(@event));
            }

            dataWriter.Write((ushort)events.Count);

            foreach (var @event in events)
            {
                dataWriter.WriteEnumAsByte(@event.Type);
                SaveEvent(dataWriter, @event);
                dataWriter.Write((ushort)(@event.Next == null ? 0xffff : events.IndexOf(@event.Next)));
            }
        }

        static void SaveEvent(IDataWriter dataWriter, Event @event)
        {
            switch (@event.Type)
            {
                case EventType.MapChange:
                {
                    // 1. byte is the x coordinate
                    // 2. byte is the y coordinate
                    // 3. byte is the character direction
                    // Then 2 unknown bytes
                    // Then a word for the map index
                    // Then 2 unknown bytes (seem to be 00 FF)
                    var mapChangeEvent = @event as MapChangeEvent;
                    dataWriter.Write((byte)mapChangeEvent.X);
                    dataWriter.Write((byte)mapChangeEvent.Y);
                    dataWriter.WriteEnumAsByte(mapChangeEvent.Direction);
                    dataWriter.Write(mapChangeEvent.Unknown1);
                    dataWriter.Write((ushort)mapChangeEvent.MapIndex);
                    dataWriter.Write(mapChangeEvent.Unknown2);
                    break;
                }
                case EventType.Door:
                {
                    // 1. byte is unknown (maybe the lock flags like for chests?)
                    // 2. byte is unknown
                    // 3. byte is unknown
                    // 4. byte is unknown
                    // 5. byte is unknown
                    // word at position 6 is the key index if a key must unlock it
                    // last word is the event index (0-based) of the event that is called when unlocking fails
                    var doorEvent = @event as DoorEvent;
                    dataWriter.Write(doorEvent.Unknown);
                    dataWriter.Write((ushort)doorEvent.KeyIndex);
                    dataWriter.Write((ushort)doorEvent.UnlockFailedEventIndex);
                    break;
                }
                case EventType.Chest:
                {
                    // 1. byte are the lock flags
                    // 2. byte is unknown (always 0 except for one chest with 20 blue discs which has 0x32 and lock flags of 0x00)
                    // 3. byte is unknown (0xff for unlocked chests)
                    // 4. byte is the chest index (0-based)
                    // 5. byte (0 = chest, 1 = pile/removable loot or item) or "remove if empty"
                    // word at position 6 is the key index if a key must unlock it
                    // last word is the event index (0-based) of the event that is called when unlocking fails
                    var chestEvent = @event as ChestEvent;
                    dataWriter.WriteEnumAsByte(chestEvent.Lock);
                    dataWriter.Write(chestEvent.Unknown);
                    dataWriter.Write((byte)chestEvent.ChestIndex);
                    dataWriter.Write((ushort)chestEvent.KeyIndex);
                    dataWriter.Write((ushort)chestEvent.UnlockFailedEventIndex);
                    break;
                }
                case EventType.PopupText:
                {
                    // event image index (0xff = no image)
                    // trigger (1 = move, 2 = cursor, 3 = both)
                    // 1 unknown byte
                    // map text index as word
                    // 4 unknown bytes
                    var textEvent = @event as PopupTextEvent;
                    dataWriter.Write((byte)textEvent.EventImageIndex);
                    dataWriter.WriteEnumAsByte(textEvent.PopupTrigger);
                    dataWriter.Write(textEvent.Unknown1);
                    dataWriter.Write((ushort)textEvent.TextIndex);
                    dataWriter.Write(textEvent.Unknown2);
                    break;
                }
                case EventType.Spinner:
                {
                    var spinnerEvent = @event as SpinnerEvent;
                    dataWriter.WriteEnumAsByte(spinnerEvent.Direction);
                    dataWriter.Write(spinnerEvent.Unknown);
                    break;
                }
                case EventType.Trap:
                {
                    var trapEvent = @event as TrapEvent;
                    dataWriter.WriteEnumAsByte(trapEvent.TypeOfTrap);
                    dataWriter.WriteEnumAsByte(trapEvent.Target);
                    dataWriter.Write(trapEvent.Value);
                    dataWriter.Write(trapEvent.Unknown);
                    dataWriter.Write(trapEvent.Unused);
                    break;
                }
                case EventType.Riddlemouth:
                {
                    var riddleMouthEvent = @event as RiddlemouthEvent;
                    dataWriter.Write((byte)riddleMouthEvent.RiddleTextIndex);
                    dataWriter.Write((byte)riddleMouthEvent.SolutionTextIndex);
                    dataWriter.Write(riddleMouthEvent.Unknown);
                    dataWriter.Write((ushort)riddleMouthEvent.CorrectAnswerDictionaryIndex);
                    break;
                }
                case EventType.Award:
                {
                    var awardEvent = @event as AwardEvent;
                    dataWriter.WriteEnumAsByte(awardEvent.TypeOfAward);
                    dataWriter.WriteEnumAsByte(awardEvent.Operation);
                    dataWriter.Write((byte)(awardEvent.Random ? 1 : 0));
                    dataWriter.WriteEnumAsByte(awardEvent.Target);
                    dataWriter.Write(awardEvent.Unknown);
                    dataWriter.Write(awardEvent.AwardTypeValue);
                    dataWriter.Write((ushort)awardEvent.Value);
                    break;
                }
                case EventType.ChangeTile:
                {
                    var changeTileEvent = @event as ChangeTileEvent;
                    byte[] tileData = new byte[4];
                    tileData[0] = (byte)(changeTileEvent.BackTileIndex & 0xff);
                    tileData[1] = (byte)(((changeTileEvent.BackTileIndex >> 8) & 0x07) << 5);
                    tileData[2] = (byte)((changeTileEvent.FrontTileIndex >> 8) & 0x07);
                    tileData[3] = (byte)(changeTileEvent.FrontTileIndex & 0xff);
                    dataWriter.Write((byte)changeTileEvent.X);
                    dataWriter.Write((byte)changeTileEvent.Y);
                    dataWriter.Write(changeTileEvent.Unknown);
                    dataWriter.Write(tileData);
                    dataWriter.Write((ushort)changeTileEvent.MapIndex);
                    break;
                }
                case EventType.StartBattle:
                {
                    var startBattleEvent = @event as StartBattleEvent;
                    dataWriter.Write(startBattleEvent.Unknown1);
                    dataWriter.Write((byte)startBattleEvent.MonsterGroupIndex);
                    dataWriter.Write(startBattleEvent.Unknown2);
                    break;
                }
                case EventType.Condition:
                {
                    var conditionEvent = @event as ConditionEvent;
                    dataWriter.WriteEnumAsByte(conditionEvent.TypeOfCondition);
                    dataWriter.Write((byte)conditionEvent.Value);
                    dataWriter.Write(conditionEvent.Unknown1);
                    dataWriter.Write((byte)conditionEvent.ObjectIndex);
                    dataWriter.Write((ushort)conditionEvent.ContinueIfFalseWithMapEventIndex);
                    break;
                }
                case EventType.Action:
                {
                    var actionEvent = @event as ActionEvent;
                    dataWriter.WriteEnumAsByte(actionEvent.TypeOfAction);
                    dataWriter.Write((byte)actionEvent.Value);
                    dataWriter.Write(actionEvent.Unknown1);
                    dataWriter.Write((byte)actionEvent.ObjectIndex);
                    dataWriter.Write(actionEvent.Unknown2);
                    break;
                }
                case EventType.Dice100Roll:
                {
                    var dice100Event = @event as Dice100RollEvent;
                    dataWriter.Write((byte)dice100Event.Chance);
                    dataWriter.Write(dice100Event.Unused);
                    dataWriter.Write((ushort)dice100Event.ContinueIfFalseWithMapEventIndex);
                    break;
                }
                case EventType.Conversation:
                {
                    var conversationEvent = @event as ConversationEvent;
                    dataWriter.WriteEnumAsByte(conversationEvent.Interaction);
                    dataWriter.Write(conversationEvent.Unused1);
                    dataWriter.Write(conversationEvent.Value);
                    dataWriter.Write(conversationEvent.Unused2);
                    break;
                }
                case EventType.PrintText:
                {
                    var printTextEvent = @event as PrintTextEvent;
                    dataWriter.Write((byte)printTextEvent.NPCTextIndex);
                    dataWriter.Write(printTextEvent.Unused);
                    break;
                }
                case EventType.Decision:
                {
                    var decisionEvent = @event as DecisionEvent;
                    dataWriter.Write((byte)decisionEvent.TextIndex);
                    dataWriter.Write(decisionEvent.Unknown1);
                    dataWriter.Write((ushort)decisionEvent.NoEventIndex);
                    break;
                }
                case EventType.ChangeMusic:
                {
                    var musicEvent = @event as ChangeMusicEvent;
                    dataWriter.Write((ushort)musicEvent.MusicIndex);
                    dataWriter.Write(musicEvent.Volume);
                    dataWriter.Write(musicEvent.Unknown1);                    
                    break;
                }
                case EventType.Exit:
                {
                    var exitEvent = @event as ExitEvent;
                    dataWriter.Write(exitEvent.Unused);
                    break;
                }
                default:
                {
                    var debugEvent = @event as DebugEvent;
                    dataWriter.Write(debugEvent.Data);
                    break;
                }
            }
        }
    }
}
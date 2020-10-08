﻿using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon.Render
{
    using Flags = Map.CharacterReference.Flags;

    // TODO: set game.MonsterSeesPlayer = true; if a monster can see the player

    internal class MapCharacter2D : Character2D, IMapCharacter
    {
        static readonly Position NullOffset = new Position(0, 0);
        readonly Game game;
        readonly Map map;
        readonly Tileset tileset;
        readonly Map.CharacterReference characterReference;
        uint lastTimeSlot = 0;

        public bool IsNPC { get; }

        // TODO: This is stored in NPC_gfx.amb.
        static readonly uint[] NumNPCFrames = new uint[]
        {
            3, 3, 6, 4, 5, 3, 3, 3, 4, 3,
            3, 4, 3, 3, 3, 2, 2, 1, 3, 3,
            4, 4, 2, 2, 2, 3, 3, 3, 3, 6,
            3, 2, 3, 2
        };

        private MapCharacter2D(Game game, IRenderView renderView, Layer layer,
            IMapManager mapManager, RenderMap2D map, Map.CharacterReference characterReference)
            : base(game, renderView.GetLayer(layer), TextureAtlasManager.Instance.GetOrCreate(layer),
                renderView.SpriteFactory, () => AnimationProvider(game, map.Map, mapManager, characterReference),
                map, GetStartPosition(characterReference), () => GetPaletteIndex(game, map.Map, characterReference),
                () => NullOffset)
        {
            this.game = game;
            this.map = map.Map;
            tileset = mapManager.GetTilesetForMap(this.map);
            this.characterReference = characterReference;
            IsNPC = !characterReference.CharacterFlags.HasFlag(Flags.UseTileset);
            lastTimeSlot = game.GameTime.TimeSlot;
        }

        static Position GetStartPosition(Map.CharacterReference characterReference)
        {
            var position = characterReference.Positions[0];

            // The positions are stored 1-based.
            return new Position(position.X - 1, position.Y - 1);
        }

        static Character2DAnimationInfo AnimationProvider(Game game, Map map, IMapManager mapManager, Map.CharacterReference characterReference)
        {
            bool usesTileset = characterReference.CharacterFlags.HasFlag(Flags.UseTileset);

            if (usesTileset)
            {
                var tileset = mapManager.GetTilesetForMap(map);
                var tile = tileset.Tiles[characterReference.GraphicIndex - 1];

                return new Character2DAnimationInfo
                {
                    FrameWidth = RenderMap2D.TILE_WIDTH,
                    FrameHeight = RenderMap2D.TILE_HEIGHT,
                    StandFrameIndex = tile.GraphicIndex - 1,
                    SitFrameIndex = 0,
                    SleepFrameIndex = 0,
                    NumStandFrames = (uint)tile.NumAnimationFrames,
                    NumSitFrames = 0,
                    NumSleepFrames = 0,
                    TicksPerFrame = map.TicksPerAnimationFrame * 2,
                    NoDirections = true,
                    IgnoreTileType = true
                };
            }
            else
            {
                var playerAnimationInfo = game.GetPlayerAnimationInfo();
                return new Character2DAnimationInfo
                {
                    FrameWidth = 16, // NPC width
                    FrameHeight = 32, // NPC height
                    StandFrameIndex = Graphics.NPCGraphicOffset + characterReference.GraphicIndex,
                    SitFrameIndex = playerAnimationInfo.SitFrameIndex,
                    SleepFrameIndex = playerAnimationInfo.SleepFrameIndex,
                    NumStandFrames = NumNPCFrames[characterReference.GraphicIndex],
                    NumSitFrames = playerAnimationInfo.NumSitFrames,
                    NumSleepFrames = playerAnimationInfo.NumSleepFrames,
                    TicksPerFrame = map.TicksPerAnimationFrame * 2,
                    NoDirections = true,
                    IgnoreTileType = false
                };
            }
        }

        static uint GetPaletteIndex(Game game, Map map, Map.CharacterReference characterReference)
        {
            return characterReference.CharacterFlags.HasFlag(Flags.UseTileset)
                ? map.PaletteIndex : game.GetPlayerPaletteIndex();
        }

        public static MapCharacter2D Create(Game game, IRenderView renderView,
            IMapManager mapManager, RenderMap2D map, Map.CharacterReference characterReference)
        {
            var layer = characterReference.CharacterFlags.HasFlag(Flags.UseTileset)
                ? (Layer)((uint)Layer.MapForeground1 + map.Map.TilesetOrLabdataIndex - 1) : Layer.Characters;
            return new MapCharacter2D(game, renderView, layer, mapManager, map, characterReference);
        }

        public override void Update(uint ticks, ITime gameTime)
        {
            if (!Active)
                return;

            Position newPosition = Position;

            void MoveRandom()
            {
                while (true)
                {
                    newPosition = new Position(Position.X + game.RandomInt(-1, 1), Position.Y + game.RandomInt(-1, 1));

                    var collisionPosition = new Position(newPosition.X, newPosition.Y + (IsNPC ? 1 : 0));

                    if (collisionPosition.X < 0 || collisionPosition.X >= map.Width)
                        continue;

                    if (collisionPosition.Y < 0)
                        continue;

                    if (collisionPosition.Y >= map.Height - (IsNPC ? 1 : 0))
                        continue;

                    if (newPosition != Position && Map[collisionPosition].AllowMovement(tileset, TravelType.Walk))
                        break;
                }
            }

            if (characterReference.Type == CharacterType.Monster)
            {
                // TODO: if the monsters see the player, they should run towards him

                if (characterReference.CharacterFlags.HasFlag(Flags.RandomMovement))
                {
                    if (lastTimeSlot != gameTime.TimeSlot)
                    {
                        MoveRandom();
                        lastTimeSlot = gameTime.TimeSlot;
                    }
                }
                else
                {
                    // Just stay
                }
            }
            else
            {
                if (characterReference.CharacterFlags.HasFlag(Flags.RandomMovement))
                {
                    if (lastTimeSlot != gameTime.TimeSlot)
                    {
                        MoveRandom();
                        lastTimeSlot = gameTime.TimeSlot;
                    }
                }
                else
                {
                    // Walk a given path every day time slot
                    newPosition = new Position(characterReference.Positions[(int)gameTime.TimeSlot]);
                    newPosition.Offset(-1, -1); // positions are 1-based

                    if (newPosition.Y != -1 && IsNPC)
                        --newPosition.Y;
                }
            }

            base.MoveTo(map, (uint)newPosition.X, (uint)newPosition.Y, ticks, false, null);
            base.Update(ticks, gameTime);
        }

        public bool Interact(EventTrigger trigger, bool bed)
        {
            switch (trigger)
            {
                case EventTrigger.Eye:
                case EventTrigger.Mouth:
                case EventTrigger.Hand:
                    if (characterReference.Type == CharacterType.Monster)
                        return false;
                    break;
                case EventTrigger.Move:
                    if (characterReference.Type != CharacterType.Monster)
                        return false;
                    break;
                default:
                    return false;
            }

            if (trigger == EventTrigger.Mouth && bed &&
                !characterReference.CharacterFlags.HasFlag(Flags.UseTileset))
            {
                game.ShowMessagePopup(game.DataNameProvider.PersonAsleepMessage);
                return true;
            }

            if (characterReference.CharacterFlags.HasFlag(Flags.TextPopup))
            {
                if (trigger == EventTrigger.Eye)
                {
                    // Popup NPCs can't be looked at but only talked to.
                    return false;
                }
                else if (trigger == EventTrigger.Mouth)
                {
                    ShowPopup(map.Texts[(int)characterReference.Index]);
                    return true;
                }
            }

            bool HandleConversation(IConversationPartner conversationPartner)
            {
                if (trigger == EventTrigger.Eye)
                {
                    game.ShowTextPopup(game.ProcessText(conversationPartner.Texts[0]), null);
                    return true;
                }
                else if (trigger == EventTrigger.Mouth)
                {
                    if (conversationPartner == null)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid NPC or party member index.");

                    conversationPartner.ExecuteEvents(game, trigger);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            switch (characterReference.Type)
            {
                case CharacterType.PartyMember:
                    return HandleConversation(game.CurrentSavegame.PartyMembers[(int)characterReference.Index]);
                case CharacterType.NPC:
                    return HandleConversation(game.CharacterManager.GetNPC(characterReference.Index));
                case CharacterType.Monster:
                    // TODO
                    break;
                case CharacterType.MapObject:
                    // TODO
                    break;
            }

            return true;
        }

        void ShowPopup(string text)
        {
            game.ShowTextPopup(game.ProcessText(text), null);
        }
    }
}
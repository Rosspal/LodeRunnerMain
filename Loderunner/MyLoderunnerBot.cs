/*-
 * #%L
 * Codenjoy - it's a dojo-like platform from developers to developers.
 * %%
 * Copyright (C) 2018 Codenjoy
 * %%
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public
 * License along with this program.  If not, see
 * <http://www.gnu.org/licenses/gpl-3.0.html>.
 * #L%
 */
using System;
using Loderunner.Api;
using System.Collections.Generic;

namespace Loderunner
{
    /// <summary>
    /// This is LoderunnerAI client demo.
    /// </summary>
    internal class MyLoderunnerBot : LoderunnerBase
    {
        int MAP_HEIGHT;
        int MAP_WIDTH;
        bool checkPortal = false;

        Stack<LoderunnerAction> acts = new Stack<LoderunnerAction>();

        public MyLoderunnerBot(string serverUrl)
            : base(serverUrl)
        {
        }

        /// <summary>
        /// Called each game tick to make decision what to do (next move)
        /// </summary>
        protected override string DoMove(GameBoard gameBoard)
        {
            //Just print current state (gameBoard) to console
            Console.Clear();
            if (gameBoard.IsHeroDead()) acts = new Stack<LoderunnerAction>();

            if (checkPortal)
            {
                acts.Clear();
                checkPortal = false;
            }

            //TODO: Implement your logic here
            BoardPoint myPos = gameBoard.GetMyPosition();
            if (acts.Count == 0) acts = FindWay(createMap(gameBoard), gameBoard);
            if (acts.Count == 0) {
                
                if (gameBoard.GetAt(myPos.Y - 1, myPos.X).Equals(BoardElement.Ladder) 
                    || gameBoard.GetAt(myPos.Y + 1, myPos.X).Equals(BoardElement.Ladder)) {
                        return LoderunnerActionToString(LoderunnerAction.DoNothing);
                    }
                Random random = new Random(Environment.TickCount);
                return LoderunnerActionToString((LoderunnerAction)random.Next(3));
            }
            

            LoderunnerAction action = acts.Pop();

            BoardPoint myPos1 = gameBoard.GetMyPosition();
            BoardPoint myPos2 = new BoardPoint(myPos.Y, myPos.X);
            switch (action)
            {
                case LoderunnerAction.GoDown:
                    if (gameBoard.GetAt(myPos2.X + 1, myPos2.Y).Equals(BoardElement.Portal))
                    {
                        checkPortal = true;
                    }
                    break;
                case LoderunnerAction.GoUp:
                    if (gameBoard.GetAt(myPos2.X - 1, myPos2.Y).Equals(BoardElement.Portal))
                    {
                        checkPortal = true;
                    }
                    break;
                case LoderunnerAction.GoLeft:
                    if (gameBoard.GetAt(myPos2.X, myPos2.Y - 1).Equals(BoardElement.Portal))
                    {
                        checkPortal = true;
                    }
                    break;
                case LoderunnerAction.GoRight:
                    if (gameBoard.GetAt(myPos2.X, myPos2.Y + 1).Equals(BoardElement.Portal))
                    {
                        checkPortal = true;
                    }
                    break;
            }

            Console.WriteLine(action);
            return LoderunnerActionToString(action);
        }

        /// <summary>
        /// Starts loderunner's client shutdown.
        /// </summary>
        public void InitiateExit()
        {
            _cts.Cancel();
        }

        public int[,] createMap(GameBoard gameBoard)
        {
            MAP_HEIGHT = gameBoard.Size;
            MAP_WIDTH = gameBoard.Size;
            int[,] map = new int[MAP_HEIGHT, MAP_WIDTH];

            String inputMap = gameBoard.BoardString;

            for (int x = 0; x < MAP_HEIGHT; x++)
                for (int y = 0; y < MAP_WIDTH; y++)
                {
                    switch(inputMap[gameBoard.GetShiftByPoint(x, y)]) {
                        case '⊛':
                        case '☼': map[x, y] = -2; break; //steni
                        case 'H': map[x,y] = -6; break; //lestnitsa
                        case '~': map[x, y] = -4; break;
                        case ' ': {
                            if (inputMap[gameBoard.GetShiftByPoint(x + 1, y)] == '☼'
                                || inputMap[gameBoard.GetShiftByPoint(x + 1, y)] == '#'
                                || inputMap[gameBoard.GetShiftByPoint(x + 1, y)] == 'H') {
                                    map[x, y] = -1; //vozduh nad stenami
                                }
                                else map[x, y] = -4;
                            break;
                        }
                        case 'S':
                        case '@':
                        case '&':
                        case '$': map[x, y] = -3; break; //zoloto
                        case '#': if (map[x - 1, y] == -6 || map[x - 1, y] == -5 || map[x - 1, y] == -10) map[x, y] = -2;
                                    else map[x, y] = -5; break; 
                        default: if (inputMap[gameBoard.GetShiftByPoint(x + 1, y)] == 'H' && inputMap[gameBoard.GetShiftByPoint(x - 1, y)] == 'H') {
                            map[x, y] = -6; 
                        }
                        else map[x, y] = -1;
                        break;
                    }
                }

                List<BoardPoint> otherPos = gameBoard.GetOtherHeroPositions();
                otherPos.AddRange(gameBoard.GetEnemyPositions());
                foreach (var other in otherPos) {
                    map[other.Y, other.X] = -2;
                }
            return map;
        }

        public Stack<LoderunnerAction> FindWay(int[,] map, GameBoard gameBoard)
        {
            BoardPoint heroPos = gameBoard.GetMyPosition();
            BoardPoint gold = new BoardPoint(1, 1);
            map[heroPos.Y, heroPos.X] = 0;
            bool notFoundYet = true;
            bool check = false;
            int step = 0;

            while (notFoundYet) {
                for (int x = 0; x < MAP_HEIGHT && !check; x++) {
                    for (int y = 0; y < MAP_WIDTH && !check; y++) {
                        if (map[x, y] == step) {
                            if (x - 1 >= 0 && map[x - 1, y] != -2 && map[x - 1, y] < 0)
                            {
                                switch(map[x - 1, y]) {
                                    case -3: 
                                        if(gameBoard.GetAt(x, y).Equals(BoardElement.Ladder)) {
                                            check = true;  
                                            gold = new BoardPoint(y, x - 1); 
                                            map[x - 1, y] = step + 1;
                                        }
                                        break;
                                    case -1: map[x - 1, y] = step + 1; break;
                                    case -6: if (gameBoard.GetAt(x, y).Equals(BoardElement.Ladder)) map[x - 1, y] = step + 1; break;
                                }
                            }
                            if (y - 1 >= 0 && map[x, y - 1] != -2 && map[x, y - 1] < 0) 
                            {
                                if (map[x + 1, y] == -2 || map[x + 1, y] == -5 || map[x + 1, y] == -10 || map[x + 1, y] == -6 || map[x + 1, y] > 0) {
                                    switch(map[x, y - 1]) {
                                        case -3: 
                                            if(!(gameBoard.GetAt(x, y).Equals(BoardElement.Ladder)) || map[x + 1, y - 1] == -2 || map[x + 1, y - 1] == -5 || map[x + 1, y - 1] == -10 || map[x + 1, y - 1] ==-6) {
                                                check = true;
                                                gold = new BoardPoint(y - 1, x);
                                                map[x, y - 1] = step + 1;
                                            }
                                            break;
                                        case -1: map[x, y - 1] = step + 1; break;
                                        case -4: if (!(gameBoard.GetAt(x, y).Equals(BoardElement.Ladder))) map[x, y - 1] = step + 1; break;
                                        case -6: map[x, y - 1] = step + 1; break;
                                        case -10: map[x, y - 1] = step + 1; break;
                                    }
                                }
                            }
                            if (y + 1 < MAP_HEIGHT && map[x, y + 1] != -2 && map[x, y + 1] < 0)
                            {
                                if (map[x + 1, y] == -2 || map[x + 1, y] == -5 || map[x + 1, y] == -10 || map[x + 1, y] == -6 || map[x + 1, y] > 0) {
                                    switch(map[x, y + 1]) {
                                        case -3: 
                                            if (!(gameBoard.GetAt(x, y).Equals(BoardElement.Ladder)) || map[x + 1, y - 1] == -2 || map[x + 1, y - 1] == -5 || map[x + 1, y - 1] == -10 || map[x + 1, y - 1] ==-6) {
                                                check = true;
                                                gold = new BoardPoint(y + 1, x);
                                                map[x, y + 1] = step + 1;
                                            }
                                            break;
                                        case -1: map[x, y + 1] = step + 1; break;
                                        case -4: if (!(gameBoard.GetAt(x, y).Equals(BoardElement.Ladder))) map[x, y + 1] = step + 1; break;
                                        case -6: map[x, y + 1] = step + 1; break;
                                        case -10: map[x, y + 1] = step + 1; break;
                                    }
                                }
                            }
                            if (x + 1 < MAP_WIDTH && map[x + 1, y] != -2 && map[x + 1, y] < 0)
                            {
                                switch(map[x + 1, y]) {
                                    case -3: 
                                        check = true;
                                        map[x + 1, y] = step + 1;
                                        gold = new BoardPoint(y, x + 1); 
                                        break;
                                    case -1: map[x + 1, y] = step + 1; break;
                                    case -4: map[x + 1, y] = step + 1; break;
                                    case -6: map[x + 1, y] = step + 1; break;
                                    case -10: map[x + 1, y] = step + 1; break;
                                }
                            }

                            if(x + 1 < MAP_WIDTH && y + 1 < MAP_HEIGHT && map[x + 1, y + 1] == -5) {
                                map[x + 1, y + 1] = -10; //mozhno prokopat
                            }

                            if(x + 1 < MAP_WIDTH && y - 1 < MAP_HEIGHT && map[x + 1, y - 1] == -5) {
                                map[x + 1, y - 1] = -10;
                            }
                        }
                    }
                }
                step++;
                if (check || step > MAP_WIDTH * MAP_HEIGHT) notFoundYet = false;
                else notFoundYet = true; 
            }
            draw(map);
            return choiceOfDirections(map, gameBoard.GetMyPosition(), gold, gameBoard);
        }

        public void draw(int[,] cMap) {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                for (int x = 0; x < MAP_WIDTH; x++) {
                    if (cMap[y, x] == -1 || cMap[y, x] == -4)  Console.Write(" ".PadRight(2));
                    else
                        if (cMap[y, x] == -2) Console.Write("☼".PadRight(2));
                        else if (cMap[y, x] == 0) Console.Write("S".PadRight(2));
                            else if(cMap[y,x] == -3) Console.Write("G".PadRight(2));
                                else if (cMap[y, x] > -1) Console.Write(cMap[y, x].ToString().PadRight(2));
                                    else if (cMap[y, x] == -5 || cMap[y, x] == -10) Console.Write("#".PadRight(2));
                                        else if (cMap[y,x] == -6) Console.Write("H".PadRight(2));
                }
                Console.WriteLine();

            }
        }

        private Stack<LoderunnerAction> choiceOfDirections(int[,] map, BoardPoint hero, BoardPoint gold, GameBoard gameBoard)
        {
            Stack<LoderunnerAction> way = new Stack<LoderunnerAction>();
            BoardPoint tempPos = new BoardPoint(gold.Y, gold.X);
            BoardPoint heroPos = new BoardPoint(hero.Y, hero.X);
            BoardPoint minPos = new BoardPoint(1, 1);
            LoderunnerAction actionDrill = LoderunnerAction.DoNothing;
            LoderunnerAction action = LoderunnerAction.DoNothing;
            int min = map[tempPos.X, tempPos.Y];

            while (tempPos != heroPos) {
                action = LoderunnerAction.DoNothing;
                actionDrill = LoderunnerAction.DoNothing;

                if (map[tempPos.X - 1, tempPos.Y] >= 0)
                {
                        if (map[tempPos.X - 1, tempPos.Y] < min)
                        {
                            if (gameBoard.GetAt(tempPos.X, tempPos.Y).Equals(BoardElement.Brick)) { 
                                    if ((min - 2) == map[tempPos.X - 1, tempPos.Y + 1]) {
                                        action = LoderunnerAction.GoLeft;
                                        actionDrill = LoderunnerAction.DrillLeft;
                                        min = min - 2;
                                        minPos = new BoardPoint(tempPos.X - 1, tempPos.Y + 1);
                                    }
                                    else if ((min - 2) == map[tempPos.X - 1, tempPos.Y - 1]) {
                                        action = LoderunnerAction.GoRight;
                                        actionDrill = LoderunnerAction.DrillRight;
                                        min = min - 2;
                                        minPos = new BoardPoint(tempPos.X - 1, tempPos.Y - 1);
                                    }

                            }
                            else {
                                    min = map[tempPos.X - 1, tempPos.Y];
                                    
                                    minPos = new BoardPoint(tempPos.X - 1, tempPos.Y);
                                    action = LoderunnerAction.GoDown;
                            }
                    }
                    
                    
                }
                if (map[tempPos.X + 1, tempPos.Y] >= 0) 
                {
                    if (map[tempPos.X + 1, tempPos.Y] > 0 || 
                        (map[tempPos.X + 1, tempPos.Y] == 0 && (gameBoard.GetAt(tempPos.X + 1, tempPos.Y).Equals(BoardElement.Ladder))))
                    {
                        if (map[tempPos.X + 1, tempPos.Y] < min)
                        {
                            min = map[tempPos.X + 1, tempPos.Y];

                            minPos = new BoardPoint(tempPos.X + 1, tempPos.Y);
                            action = LoderunnerAction.GoUp;
                        }
                    }
                }

                if (map[tempPos.X, tempPos.Y + 1] >= 0)
                {
                    if (map[tempPos.X, tempPos.Y + 1] < min)
                    {
                        min = map[tempPos.X, tempPos.Y + 1];

                        minPos = new BoardPoint(tempPos.X, tempPos.Y + 1);
                        action = LoderunnerAction.GoLeft;
                    }
                }

                if (map[tempPos.X, tempPos.Y - 1] >= 0)
                {
                    if (map[tempPos.X, tempPos.Y - 1] < min)
                    {
                        min = map[tempPos.X, tempPos.Y - 1];

                        minPos = new BoardPoint(tempPos.X, tempPos.Y - 1);
                        action = LoderunnerAction.GoRight;
                    }
                }

                if (action == LoderunnerAction.DoNothing) {
                    return new Stack<LoderunnerAction>();
                }

                tempPos = minPos;
                way.Push(action);
                if (actionDrill != LoderunnerAction.DoNothing) {
                    way.Push(actionDrill);
                }
            }
            return way;
        }
    }
}

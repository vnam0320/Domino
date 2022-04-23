using System.Collections.Generic;
using System.Linq;

namespace DominoC
{
    class MFPlayer
    {
        // Место, где можем положить доминушку: в начале ряда доминушков, в конце, или обо 
        enum BonePlaces { start = 0, end, both};
        const int MaxPointBone = 25;

        static public string PlayerName = "VuNam";
        static private List<MTable.SBone> lHand;

        //=== Готовые функции =================
        // инициализация игрока
        static public void Initialize()
        {
            lHand = new List<MTable.SBone>();
        }
        // Вывод на экран
        static public void PrintAll()
        { MTable.PrintAll(lHand); }
        // дать количество доминушек
        static public int GetCount()
        { return lHand.Count; }
        //=== Функции разработки =================
        // сделать ход        
        static public bool MakeStep(out MTable.SBone sb, out bool End)
        {
            int Fisrt = MTable.GetGameCollection()[0].First;
            int Last = MTable.GetGameCollection().Last().Second;

            sb = new MTable.SBone();

            Dictionary<BonePlaces, List<MTable.SBone>> usefulBones = GetUsefulBone(lHand, Fisrt, Last, out bool isHaveAnyOne);

            // если нет доминушки, которую можем положить то нужно брать из базара
            while (!isHaveAnyOne)
            {
                // до тех пор, пока не сделаем ход 
                if (MTable.GetFromShop(out MTable.SBone newCard))
                {
                    AddItem(newCard);
                }
                // или в базаре не закончатся доминушки
                else
                {
                    End = false;
                    return false;
                }

                usefulBones = GetUsefulBone(lHand, Fisrt, Last, out isHaveAnyOne);
            }
            // из полезных доминушков выбрать доминушки, после положить которые на след. ход мы можем ходить другие
            ContinueBone(ref usefulBones, Fisrt, Last);
            // отсортировать по правилу: доминушки в начале имеют более приоритет, чем в конце
            ArrangeBones(usefulBones);

            // предпочитаем доминушки, которые можем положить и в начале и в конце,
            // так как после того, что положил их, ряд доминушков будет в таком виде n:------:n
            // то есть у конкурента будет меньше выбор

            if (usefulBones[BonePlaces.both].Count > 0)
            {
                sb = usefulBones[BonePlaces.both].First();
                // определить где лучше положить: в конце или в начале
                End = WhenBothValid(sb, Fisrt, Last);
            }
            else if (usefulBones[BonePlaces.start].Count > 0)
            {
                End = false;
                sb = usefulBones[BonePlaces.start].First();

                if (usefulBones[BonePlaces.end].Count > 0)
                {
                    // определить где лучше положить: в конце или в начале

                    MTable.SBone startBone = usefulBones[BonePlaces.start].First();
                    MTable.SBone endBone = usefulBones[BonePlaces.end].First();

                    if (TotalPointBone(startBone) < TotalPointBone(endBone))
                    {
                        End = true;
                        sb = endBone;
                    }
                    else if(TotalPointBone(startBone) == TotalPointBone(endBone))
                    {
                        int[] variety = CountVariety();

                        int startVariety = variety[startBone.First];
                        if(variety[startBone.First] > variety[startBone.Second])
                        {
                            startVariety = variety[startBone.Second];
                        }

                        int endVariety = variety[endBone.First];
                        if (variety[endBone.First] > variety[endBone.Second])
                        {
                            endVariety = variety[startBone.Second];
                        }

                        if(startVariety > endVariety)
                        {
                            End = true;
                            sb = endBone;
                        }
                    }
                }
            }
            else
            {
                End = true;
                sb = usefulBones[BonePlaces.end].First();
            }

            lHand.Remove(sb);
            return true;
        }
        // Вернуть словарь с ключами: место положить доминушки, значение: список доминушков можно в этом месте положить
        static Dictionary<BonePlaces, List<MTable.SBone>> GetUsefulBone(List<MTable.SBone> bones, int firstNumber, int lastNumber, out bool isHaveAnyOne)
        {
            // словарь состоит из списков доминушкво, которые можем положить на столе
            var validBones = new Dictionary<BonePlaces, List<MTable.SBone>>();
            // список можем положить в начале ряда доминушки
            validBones.Add(BonePlaces.start, new List<MTable.SBone>());
            // список можем положить в конце ряда доминушки
            validBones.Add(BonePlaces.end, new List<MTable.SBone>());
            // список можем положить и в начале и конце ряда доминушки
            validBones.Add(BonePlaces.both, new List<MTable.SBone>());

            // ряд доминушков будет таким видом: firtNumber:--------:lastNumber
            // доминушки, которые можем положить в начале, то у них есть хотя бы один совподает с firstNumber
            // доминушки, которые можем положить в конце, то у них есть хотя бы один совподает с lastNumber
            // доминушки, которые модем положить в начале и в конце, то один номер должен совпадать с началой и другой с концом.

            bones.ForEach(bone =>
            {
                if (bone.First == firstNumber || bone.Second == firstNumber)
                {
                    if (bone.Second == lastNumber || bone.First == lastNumber)
                        validBones[BonePlaces.both].Add(bone);
                    else
                        validBones[BonePlaces.start].Add(bone);
                }
                else if (bone.First == lastNumber || bone.Second == lastNumber)
                {
                    if (bone.Second == firstNumber || bone.First == firstNumber)
                        validBones[BonePlaces.both].Add(bone);
                    else
                        validBones[BonePlaces.end].Add(bone);
                }
            });

            // если на руке нет доминушки, которой можем ходить, то isHaveAnyOne = false;
            isHaveAnyOne = CountElementDict(validBones) != 0;

            return validBones;
        }
        // Из доминушков, которые можем положить на столе, выбрать доминушки, после  того, что положил эту доминушку,
        // на следующий  ход мы можем положить другую доминушку
        static void ContinueBone(ref Dictionary<BonePlaces, List<MTable.SBone>> validCards, int First, int Last)
        {
            var continueBones = new Dictionary<BonePlaces, List<MTable.SBone>>();
            continueBones.Add(BonePlaces.both, new List<MTable.SBone>());
            continueBones.Add(BonePlaces.start, new List<MTable.SBone>());
            continueBones.Add(BonePlaces.end, new List<MTable.SBone>());

            ContinueFuntion(validCards[BonePlaces.both], continueBones[BonePlaces.both], First, Last);
            ContinueFuntion(validCards[BonePlaces.start], continueBones[BonePlaces.start], First, Last);
            ContinueFuntion(validCards[BonePlaces.end], continueBones[BonePlaces.end], First, Last);

            // если нет continue bone, то вернуть начальной список доминушков
            // если есть continue bone, то лучшие доминушки - это continue bone
            if (CountElementDict(continueBones) != 0)
            {
                validCards = continueBones;
            }
        }
        // выбрать доминушки, после  того, что положил эту доминушку, на следующий  ход мы можем положить другую доминушку
        static public void ContinueFuntion(List<MTable.SBone> input, List<MTable.SBone> output, int First, int Last)
        {
            for (int i = 0; i < input.Count; i++)
            {   // визуальный список доминушков после того, что положил одну доминишку
                var vitualHandBones = new List<MTable.SBone>();
                vitualHandBones.AddRange(lHand);
                var card = input[i];
                // когда положил доминушку, удалим его из руки
                vitualHandBones.Remove(card);
                bool isHaveAnyOne = false;

                // проверить когда эту доминушку, на следующий ход можем ходить или нет
                if (card.First == Last)
                {
                    GetUsefulBone(vitualHandBones, First, card.Second, out isHaveAnyOne);
                }
                else if (card.Second == Last)
                {
                    GetUsefulBone(vitualHandBones, First, card.First, out isHaveAnyOne);
                }
                else if (card.First == First)
                {
                    GetUsefulBone(vitualHandBones, card.Second, Last, out isHaveAnyOne);
                }
                else if (card.Second == First)
                {
                    GetUsefulBone(vitualHandBones, card.First, Last, out isHaveAnyOne);
                }

                // если можем ходить, то добавить ее в Continues bone
                if (isHaveAnyOne)
                    output.Add(card);
            }
        }
        // отсортировать доминушки по некоторым критериям
        static void ArrangeBones(Dictionary<BonePlaces, List<MTable.SBone>> usefulBones)
        {

            int[] variety = CountVariety();
            // using bubble sort
            foreach (var position in usefulBones)
            {
                var listBones = position.Value;
                // повернуть, чтобы первый номер на доминушке всегда появляются меньше, чем 2-й номер
                FisrtIsLessVariety(listBones, variety);

                for (int i = 1; i < listBones.Count; i++)
                {
                    for (int j = 0; j < listBones.Count - i; j++)
                    {
                        // если на руке только две доминушки и одна из двух это 0:0, то надо срочко удалить 0:0 из руки
                        if (lHand.Count < 3 && listBones[j].First == 0 && listBones[j].Second == 0)
                            Swap(listBones, j, j + 1);
                        // мы предпочитаем убрать доминошки, у которых высокие баллы, затем доминошки, у которые низкие
                        else if (TotalPointBone(listBones[j]) < TotalPointBone(listBones[j + 1]))
                        {
                            Swap(listBones, j, j + 1);
                        }
                        // Если две кости домино имеют одинаковый счет, то мы отдаем предпочтение тем,
                        // которые содержат цифры, которые появляются больше
                        else if (TotalPointBone(listBones[j]) == TotalPointBone(listBones[j + 1]))
                        {
                            if (variety[listBones[j].First] < variety[listBones[j + 1].First])
                            {
                                Swap(listBones, j, j + 1);
                            }
                        }

                    }
                }
            }
        }
        static public bool IsDouble(MTable.SBone bone)
        {
            return bone.First == bone.Second;
        }
        // Когда доминушку можно положить и в начале и в конце, то надо определить где положить угоднее
        static public bool WhenBothValid(MTable.SBone boneBoth, int First, int Last)
        {
            var templHand = new List<MTable.SBone>();
            templHand.AddRange(lHand);
            templHand.Remove(boneBoth);

            int newFirst, newLast;

            if (boneBoth.First == First)
            {
                newFirst = boneBoth.Second;
                newLast = boneBoth.First;
            }
            else
            {
                newFirst = boneBoth.Second;
                newLast = boneBoth.First;
            }
            var startPut = GetUsefulBone(templHand, newFirst, Last, out bool isHaveAnyOne);
            var lastPut = GetUsefulBone(templHand, First, newLast, out isHaveAnyOne);

            // если после того, что положил в начале, у нас будет больше вариантов для сред. хода, тем положил в конце
            // то конечно мы будем положить в начале
            if (CountElementDict(startPut) >= CountElementDict(lastPut))
            {
                // вернуть ложь если решил в начале положить
                return false;
            }
            else
            {
                // вернуть истина если решил в конце положить
                return true;
            }
        }
        // поменять местами элементов в списке
        static void Swap<T>(List<T> listItem, int firstItem, int secondItem)
        {
            var temp = listItem[firstItem];
            listItem[firstItem] = listItem[secondItem];
            listItem[secondItem] = temp;
        }
        // добавить доминушку в свою руку
        static public void AddItem(MTable.SBone sb)
        { lHand.Add(sb); }
        // дать сумму очков на руке
        static public int GetScore()
        {
            int myScore = 0;
            lHand.ForEach(bone =>
            {
                myScore += TotalPointBone(bone);
            });

            return myScore;
        }
        // вернуть количество нумеров (с 0 до 6) на руке
        static public int[] CountVariety()
        {
            int[] variety = new int[7];
            foreach (var bone in lHand)
            {
                variety[bone.First]++;
                variety[bone.Second]++;
            }

            return variety;
        }
        // принимать список доминуки и массив количества появления номеров,
        // мы вернем каждую доминушку из списка, чтобы 
        // первый номер на доминушке появляются меньше, чем 2-й номер
        static void FisrtIsLessVariety(List<MTable.SBone> bones, int[] variety)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (variety[bones[i].First] > variety[bones[i].Second]) bones[i].Exchange();
            }
        }
        // дать сумму очков одной доминушки
        static int TotalPointBone(MTable.SBone bone)
        {
            // когда осталась одна доминишка 0:0,
            // то её сумма очкво будет равна MaxPointBone(по умочанию это равно 25)
            if (GetCount() == 1 && bone.First == 0 && bone.Second == 0)
            {
                return MaxPointBone;
            }
            return bone.First + bone.Second;
        }
        // вернуть количество элементов в словаре список
        static int CountElementDict<k, li>(Dictionary<k, List<li>> dict)
        {
            int count = 0;
            foreach (var key in dict)
            {
                count += key.Value.Count;
            }
            return count;
        }
    }
}

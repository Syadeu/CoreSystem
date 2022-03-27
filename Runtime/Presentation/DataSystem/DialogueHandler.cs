//// Copyright 2022 Seung Ha Kim
//// 
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
//// 
////     http://www.apache.org/licenses/LICENSE-2.0
//// 
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.

//#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
//#define DEBUG_MODE
//#endif

//using Syadeu.Collections;
//using Syadeu.Presentation.Actor;
//using Syadeu.Presentation.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Syadeu.Presentation.Data
//{
//    public sealed class DialogueHandler
//    {
//        internal Reference<DialogueNodeData>[] m_Dialogues = null;
//        private bool m_Ended = false;
//        private int m_CurrentSelection = 0;
//        private IEnumerator<TextInfo> m_Iterator = null;

//        public TextInfo Current => m_Iterator.Current;

//        public sealed class TextInfo
//        {
//            private readonly Culture culture;
//            public readonly Entity<ActorEntity> speaker;
//            private readonly Reference<LocalizedTextData>[] textData;
//            private readonly int[] textIndices;

//            public string[] texts
//            {
//                get
//                {
//                    List<string> texts = new List<string>();
//                    for (int i = 0; i < textIndices.Length; i++)
//                    {
//                        if (!textData[i].IsValid())
//                        {
//                            CoreSystem.Logger.LogError(Channel.Entity, $"invalid textdata");
//                            continue;
//                        }
//                        LocalizedTextData.Entry entries = textData[i].GetObject().m_Entries[textIndices[i]];
//                        for (int j = 0; j < entries.m_Texts.Length; j++)
//                        {
//                            if (entries.m_Texts[j].m_Culture.Equals(culture))
//                            {
//                                texts.Add(entries.m_Texts[j].m_Text);
//                            }
//                        }

//                        CoreSystem.Logger.LogError(Channel.Entity,
//                            $"cannot found text on that culture({culture}) at {textData[i].GetObject().Name}");
//                        continue;
//                    }

//                    return texts.ToArray();
//                }
//            }

//            public TextInfo(Culture culture, Entity<ActorEntity> speaker, DialogueNodeData.Option[] option)
//            {
//                this.culture = culture;
//                this.speaker = speaker;

//                textData = option.Select((other) => other.m_TextData).ToArray();
//                textIndices = option.Select((other) => other.m_TextIndex).ToArray();
//            }
//        }

//        public DialogueHandler(Culture culture, params Entity<ActorEntity>[] entries)
//        {
//            m_Iterator = Conversation(culture, entries);

//            currentNode = m_Dialogues[currentIndex].GetObject();
//        }

//        public DialogueHandler Select(int optionIndex)
//        {
//            if (m_Ended) throw new Exception();
//            if (optionIndex >= currentNode.m_Options.Length)
//            {
//                throw new ArgumentOutOfRangeException(nameof(optionIndex));
//            }

//            m_CurrentSelection = optionIndex;
//            m_Ended = !m_Iterator.MoveNext();
//            return this;
//        }

//        int currentIndex = 0;
//        DialogueNodeData currentNode;
//        DialogueNodeData.Option currentOption;

//        private IEnumerator<TextInfo> Conversation(Culture culture, params Entity<ActorEntity>[] entries)
//        {
//            currentOption = currentNode.m_Options[m_CurrentSelection];

//            if (!TryFindEntity(currentNode.m_Speaker, out Entity<ActorEntity> targetEntity, entries))
//            {
//                CoreSystem.Logger.LogError(Channel.Entity,
//                    "unexpected conversation exit");
//                yield break;
//            }

//            TextInfo textInfo = new TextInfo(culture, targetEntity, currentNode.m_Options);
//            yield return textInfo;

//            DialogueNodeData prevNode = null;
//            DialogueNodeData.Option prevOption = null;

//            while (currentIndex < m_Dialogues.Length)
//            {
//                currentIndex++;
//                if (currentIndex >= m_Dialogues.Length) break;

//                prevNode = currentNode;
//                prevOption = currentOption;

//                currentNode = GetNode(currentIndex);
//                if (!currentNode.Predicate(prevNode, prevOption))
//                {
//                    CoreSystem.Logger.Log(Channel.Entity,
//                        "current node predicate failed. skipping");
//                    continue;
//                }
//                if (!TryFindEntity(currentNode.m_Speaker, out targetEntity, entries))
//                {
//                    CoreSystem.Logger.Log(Channel.Entity,
//                        "cannot found target entity. skipping");
//                    continue;
//                }

//                textInfo = new TextInfo(culture, targetEntity, currentNode.m_Options);
//                yield return textInfo;
//            }

//            DialogueNodeData GetNode(int index) => m_Dialogues[currentIndex].GetObject();

//            bool TryFindEntity(Reference<ActorEntity> reference, out Entity<ActorEntity> entity, params Entity<ActorEntity>[] entries)
//            {
//                entity = FindEntity(reference, entries);
//                if (entity.IsValid()) return true;
//                return false;
//            }
//            Entity<ActorEntity> FindEntity(Reference<ActorEntity> reference, params Entity<ActorEntity>[] entries)
//            {
//                var iter = entries.Where((other) => other.Hash.Equals(reference.Hash));
//                if (iter.Any())
//                {
//                    return iter.First();
//                }
//                return Entity<ActorEntity>.Empty;
//            }
//        }
//    }
//}

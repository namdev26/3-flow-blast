using System.Collections.Generic;
using FlowBlast.Presentation.Block;
using UnityEngine;

namespace FlowBlast.Patterns.Pool
{
    public sealed class BlockViewPool : IObjectPool<BlockView>
    {
        private readonly BlockView prefab;
        private readonly Transform parent;
        private readonly Queue<BlockView> availableItems = new Queue<BlockView>();

        public BlockViewPool(BlockView prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                BlockView instance = CreateInstance();
                Release(instance);
            }
        }

        public bool TryGet(out BlockView item)
        {
            if (availableItems.Count == 0)
            {
                item = CreateInstance();
                item.gameObject.SetActive(true);
                return true;
            }

            item = availableItems.Dequeue();
            item.gameObject.SetActive(true);
            return true;
        }

        public void Release(BlockView item)
        {
            if (item == null)
            {
                return;
            }

            item.ResetView();
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(false);
            availableItems.Enqueue(item);
        }

        public void Consume(BlockView item)
        {
            if (item == null)
            {
                return;
            }

            item.ResetView();
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(false);
        }

        public void RecycleAll()
        {
            availableItems.Clear();
            BlockView[] instances = parent.GetComponentsInChildren<BlockView>(true);

            for (int i = 0; i < instances.Length; i++)
            {
                instances[i].ResetView();
                instances[i].transform.SetParent(parent, false);
                instances[i].gameObject.SetActive(false);
                availableItems.Enqueue(instances[i]);
            }
        }

        private BlockView CreateInstance()
        {
            BlockView instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}

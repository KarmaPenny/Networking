using System.Collections.Generic;

namespace Networking.Network {
    public class StateList<T> : SortedDictionary<int, T> where T : new() {
        const int capacity = 300;
        const int minLag = 4;
        const int maxLag = 14;
        const int lag = 9;

        public int frame = 0;
        public int topFrame = 0;
        public List<int> Frames {
            get {
                return new List<int>(Keys);
            }
        }

        public T Get(int frame) {
            // use previous state if frame is not in list
            if (!ContainsKey(frame)) {
                // use new state if previous frame is not in list
                if (!ContainsKey(frame -1)) {
                    this[frame - 1] = new T();
                }
                this[frame] = this[frame - 1];
            }
            return this[frame];
        }

        public T GetCurrent() {
            return Get(frame);
        }

        public T GetPrevious() {
            return Get(frame - 1);
        }

        public T GetTop() {
            return Get(topFrame);
        }

        public T GetBottom() {
            return Get(topFrame - capacity + 1);
        }

        public void Append(T state) {
            Add(topFrame + 1, state);
        }

        public new void Add(int i, T state) {
            // don't out if the frame is outside of the capcity
            if (i <= topFrame - capacity) {
                return;
            }

            // insert the new state
            this[i] = state;

            // update top frame
            if (i > topFrame) {
                topFrame = i;

                // keep frame slightly behind the top frame to allow time for new frames to arrive and be sorted before being used
                if (frame < topFrame - maxLag || frame > topFrame - minLag) {
                    frame = topFrame - lag;
                }
            }

            // remove states outside of the capcity
            foreach(int f in Frames) {
                // stop when we are inside the capacity
                if (f > topFrame - capacity) {
                    break;
                }
                Remove(f);
            }
        }
    }
}
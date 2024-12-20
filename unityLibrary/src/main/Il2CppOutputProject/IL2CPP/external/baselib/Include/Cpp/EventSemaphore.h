#pragma once

#include "../C/Baselib_EventSemaphore.h"
#include "Time.h"

namespace baselib
{
    BASELIB_CPP_INTERFACE
    {
        // In computer science, an event (also called event semaphore) is a type of synchronization mechanism that is used to indicate to waiting processes when a
        // particular condition has become true.
        // An event is an abstract data type with a boolean state and the following operations:
        // * wait - when executed, causes the suspension of the executing process until the state of the event is set to true. If the state is already set to true has no effect.
        // * set - sets the event's state to true, release all waiting processes.
        // * clear - sets the event's state to false.
        //
        // "Event (synchronization primitive)", Wikipedia: The Free Encyclopedia
        // https://en.wikipedia.org/w/index.php?title=Event_(synchronization_primitive)&oldid=781517732
        //
        // For optimal performance, baselib::EventSemaphore should be stored at a cache aligned memory location.
        class EventSemaphore
        {
        public:
            // non-copyable
            EventSemaphore(const EventSemaphore& other) = delete;
            EventSemaphore& operator=(const EventSemaphore& other) = delete;

            // non-movable (strictly speaking not needed but listed to signal intent)
            EventSemaphore(EventSemaphore&& other) = delete;
            EventSemaphore& operator=(EventSemaphore&& other) = delete;

            // Creates an event semaphore synchronization primitive. Initial state of event is unset.
            //
            // If there are not enough system resources to create a semaphore, process abort is triggered.
            EventSemaphore()
            {
                Baselib_EventSemaphore_CreateInplace(&m_EventSemaphoreData);
            }

            // Reclaim resources and memory held by the semaphore.
            // If threads are waiting on the semaphore, calling free may trigger an assert and may cause process abort.
            ~EventSemaphore()
            {
                Baselib_EventSemaphore_FreeInplace(&m_EventSemaphoreData);
            }

            // Try to acquire semaphore.
            //
            // When semaphore is acquired this function is guaranteed to emit an acquire barrier.
            //
            // \param maxSpinCount  Max number of times to spin in user space before falling back to the kernel. The actual number
            //                      may differ depending on the underlying implementation but will never exceed the maxSpinCount
            //                      value.
            // \returns             true if event is set, false other wise.
            COMPILER_WARN_UNUSED_RESULT
            inline bool TryAcquire(const uint32_t maxSpinCount = 0)
            {
                return Baselib_EventSemaphore_TrySpinAcquire(&m_EventSemaphoreData, maxSpinCount);
            }

            // Acquire semaphore.
            //
            // This function is guaranteed to emit an acquire barrier.
            //
            // \param maxSpinCount  Max number of times to spin in user space before falling back to the kernel. The actual number
            //                      may differ depending on the underlying implementation but will never exceed the maxSpinCount
            //                      value.
            inline void Acquire(const uint32_t maxSpinCount = 0)
            {
                if (maxSpinCount && Baselib_EventSemaphore_TrySpinAcquire(&m_EventSemaphoreData, maxSpinCount))
                    return;

                return Baselib_EventSemaphore_Acquire(&m_EventSemaphoreData);
            }

            // Try to acquire semaphore.
            //
            // If event is set this function return true, otherwise the thread will wait for event to be set or for release to be called.
            //
            // When semaphore is acquired this function is guaranteed to emit an acquire barrier.
            //
            // Acquire with a zero timeout differs from TryAcquire in that TryAcquire is guaranteed to be a user space operation
            // while Acquire may enter the kernel and cause a context switch.
            //
            // Timeout passed to this function may be subject to system clock resolution.
            // If the system clock has a resolution of e.g. 16ms that means this function may exit with a timeout error 16ms earlier than originally scheduled.
            //
            // \param maxSpinCount  Max number of times to spin in user space before falling back to the kernel. The actual number
            //                      may differ depending on the underlying implementation but will never exceed the maxSpinCount
            //                      value.
            // \returns             true if semaphore was acquired.
            COMPILER_WARN_UNUSED_RESULT
            inline bool TryTimedAcquire(const timeout_ms timeoutInMilliseconds, const uint32_t maxSpinCount = 0)
            {
                if (maxSpinCount && Baselib_EventSemaphore_TrySpinAcquire(&m_EventSemaphoreData, maxSpinCount))
                    return true;

                return Baselib_EventSemaphore_TryTimedAcquire(&m_EventSemaphoreData, timeoutInMilliseconds.count());
            }

            // Sets the event
            //
            // Setting the event will cause all waiting threads to wakeup. And will let all future acquiring threads through until Baselib_EventSemaphore_Reset is called.
            // It is guaranteed that any thread waiting previously on the EventSemaphore will be woken up, even if the semaphore is immediately reset. (no lock stealing)
            //
            // Guaranteed to emit a release barrier.
            inline void Set()
            {
                return Baselib_EventSemaphore_Set(&m_EventSemaphoreData);
            }

            // Reset event
            //
            // Resetting the event will cause all future acquiring threads to enter a wait state.
            // Has no effect if the EventSemaphore is already in a reset state.
            //
            // Guaranteed to emit a release barrier.
            inline void Reset()
            {
                return Baselib_EventSemaphore_Reset(&m_EventSemaphoreData);
            }

            // Deprecated: Please use ResetAndReleaseWaitingThreads()
            inline void ResetAndRelease()
            {
                return Baselib_EventSemaphore_ResetAndReleaseWaitingThreads(&m_EventSemaphoreData);
            }

            // Reset event and release all waiting threads
            //
            // Resetting the event will cause all future acquiring threads to enter a wait state.
            // If there were any threads waiting (i.e. the EventSemaphore was already in a release state) they will be released.
            //
            // Guaranteed to emit a release barrier.
            inline void ResetAndReleaseWaitingThreads()
            {
                return Baselib_EventSemaphore_ResetAndReleaseWaitingThreads(&m_EventSemaphoreData);
            }

        private:
            Baselib_EventSemaphore   m_EventSemaphoreData;
        };
    }
}

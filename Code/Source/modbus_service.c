#include "modbus_service.h"
#include "modbus_proto.h"
#include <string.h>

void Modbus_ServiceInit(struct RS485MSG *msg)
{
    Sci_DataInit(msg);
}

uint16_t Modbus_ServiceHandleFrame(struct RS485MSG *msg, const uint8_t *frame, uint16_t frame_len, uint8_t *tx_buf, uint16_t tx_capacity)
{
    uint16_t i;
    uint8_t is_broadcast;

    Sci_DataInit(msg);
    if ((frame_len == 0) || (frame_len > RS485_MAX_BUFFER_SIZE))
    {
        return 0;
    }

    is_broadcast = (uint8_t)(frame[0] == MODBUS_BROADCAST_ADDR);

    for (i = 0; i < frame_len; ++i)
    {
        msg->u16Buffer[i] = frame[i];
    }
    msg->ptr_no = (uint8_t)frame_len;

    switch (msg->u16Buffer[1])
    {
    case RS485_CMD_READ_REGS:
        if (is_broadcast != 0U)
        {
            return 0;
        }
        msg->enRs485CmdType = RS485_CMD_READ_REGS;
        break;
    case RS485_CMD_WRITE_REG:
        msg->enRs485CmdType = RS485_CMD_WRITE_REG;
        break;
    case RS485_CMD_WRITE_REGS:
        msg->enRs485CmdType = RS485_CMD_WRITE_REGS;
        break;
    default:
        return 0;
    }

    Sci_ModbusService_Execute(msg);
    if (is_broadcast != 0U)
    {
        return 0;
    }

    if ((msg->AckLenth == 0) || (msg->AckLenth > RS485_MAX_BUFFER_SIZE) || (msg->AckLenth > tx_capacity))
    {
        return 0;
    }
    memcpy(tx_buf, msg->u16Buffer, msg->AckLenth);
    return msg->AckLenth;
}

import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, EmptyRow, Loading } from '../lib/ui'
import { vnd, fmtDate } from '../lib/helpers'
import type { ContractDto } from '../lib/types'

export default function MyContracts() {
  const toast = useToast()
  const [list, setList] = useState<ContractDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    (async () => {
      try { setList(await api.get<ContractDto[]>('/me/contracts')) }
      catch (e) { toast((e as Error).message, 'err') }
      finally { setLoading(false) }
    })()
  }, [toast])

  return (
    <>
      <PageHead title="Hợp đồng của tôi" />
      <div className="card">
        <table>
          <thead><tr>
            <th>Mã HĐ</th><th>Phòng</th><th>Thời hạn</th>
            <th className="num">Tiền phòng</th><th className="num">Cọc</th><th>Trạng thái</th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={6}><Loading /></td></tr>
              : list.length ? list.map(c => (
                <tr key={c.id}>
                  <td>{c.contractCode}</td><td><b>{c.roomName}</b></td>
                  <td className="small">{fmtDate(c.startDate)} → {fmtDate(c.endDate)}</td>
                  <td className="num">{vnd(c.monthlyRent)}</td><td className="num">{vnd(c.deposit)}</td>
                  <td><StatusBadge statusName={c.statusName} /></td>
                </tr>
              )) : <EmptyRow cols={6} />}
          </tbody>
        </table>
      </div>
    </>
  )
}
